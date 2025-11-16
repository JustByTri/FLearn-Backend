using BLL.IServices.Teacher;
using Common.DTO.ApiResponse;
using Common.DTO.Paging.Response;
using Common.DTO.PayOut;
using Common.DTO.Teacher;
using Common.DTO.Teacher.Request;
using Common.DTO.Teacher.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BLL.Services.Teacher
{
    public class TeacherService : ITeacherService
    {
        private readonly IUnitOfWork _unit;
        public TeacherService(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public async Task<BaseResponse<TeacherProfileResponse>> GetTeacherProfileAsync(Guid userId)
        {
            try
            {
                var user = await _unit.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<TeacherProfileResponse>.Fail(new object(), "Accessed denied", 403);

                var teacher = await _unit.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<TeacherProfileResponse>.Fail(new object(), "Accessed denied", 403);

                var language = await _unit.Languages.GetByIdAsync(teacher.LanguageId);

                var response = new TeacherProfileResponse
                {
                    TeacherId = teacher.TeacherId,
                    Language = language?.LanguageName,
                    FullName = teacher.FullName,
                    DateOfBirth = teacher.BirthDate.ToString("dd-MM-yyyy"),
                    Bio = teacher.Bio,
                    Avatar = teacher.Avatar,
                    Email = teacher.Email,
                    PhoneNumber = teacher.PhoneNumber,
                    ProficiencyCode = teacher.ProficiencyCode,
                    AverageRating = teacher.AverageRating,
                    ReviewCount = teacher.ReviewCount,
                    MeetingUrl = teacher.MeetingUrl,
                };

                return BaseResponse<TeacherProfileResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return BaseResponse<TeacherProfileResponse>.Fail(new object(), ex.Message, 500);
            }

        }
        public async Task<BaseResponse<object>> CreatePayoutRequestAsync(Guid teacherId, CreatePayoutRequestDto requestDto)
        {
            var teacherProfile = await _unit.TeacherProfiles.FindAsync(t => t.UserId == teacherId);
            if (teacherProfile == null)
            {
                return BaseResponse<object>.Fail(null, "Không tìm thấy hồ sơ giáo viên.", (int)HttpStatusCode.NotFound);
            }
            var teacherProfileId = teacherProfile.TeacherId;

            var wallet = (await _unit.Wallets.GetByConditionAsync(w => w.TeacherProfile.UserId == teacherId)).FirstOrDefault();
            if (wallet == null)
            {
                return BaseResponse<object>.Fail(null, "Không tìm thấy ví của giáo viên.", (int)HttpStatusCode.NotFound);
            }
            if (wallet.AvailableBalance < requestDto.Amount)
            {
                return BaseResponse<object>.Fail(null, "Số dư khả dụng không đủ để thực hiện yêu cầu.", (int)HttpStatusCode.BadRequest);
            }

            var bankAccount = await _unit.TeacherBankAccounts.GetByIdAsync(requestDto.BankAccountId);
            if (bankAccount == null || bankAccount.TeacherId != teacherProfileId)
            {
                return BaseResponse<object>.Fail(null, "Tài khoản ngân hàng không hợp lệ hoặc không thuộc về bạn.", (int)HttpStatusCode.BadRequest);
            }

            try
            {
                await _unit.ExecuteInTransactionAsync(async () =>
                {
                    // Adjust wallet balances
                    wallet.TotalBalance -= requestDto.Amount;
                    wallet.AvailableBalance -= requestDto.Amount;
                    wallet.UpdatedAt = TimeHelper.GetVietnamTime();

                    // Create wallet transaction
                    var newTransaction = new WalletTransaction
                    {
                        WalletTransactionId = Guid.NewGuid(),
                        WalletId = wallet.WalletId,
                        Amount = -requestDto.Amount,
                        TransactionType = TransactionType.Withdrawal,
                        Status = TransactionStatus.Pending,
                        Description = "Payout request",
                        CreatedAt = TimeHelper.GetVietnamTime(),
                        ReferenceId = null
                    };
                    await _unit.WalletTransactions.AddAsync(newTransaction);

                    // Create payout request
                    var now = TimeHelper.GetVietnamTime();
                    var newPayoutRequest = new PayoutRequest
                    {
                        TeacherId = teacherProfileId,
                        BankAccountId = bankAccount.BankAccountId,
                        Amount = requestDto.Amount,
                        PayoutStatus = PayoutStatus.Pending,
                        RequestedAt = now,
                        CreatedAt = now,
                        TransactionRef = $"WD-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToUpperInvariant(),
                        PayoutChannel = "BankTransfer",
                        Note = string.Empty
                    };
                    await _unit.PayoutRequests.AddAsync(newPayoutRequest);

                    // Link transaction to payout request
                    newTransaction.ReferenceId = newPayoutRequest.PayoutRequestId;

                    // Save all changes
                    await _unit.SaveChangesAsync();
                });

                return BaseResponse<object>.Success(null, "Gửi yêu cầu rút tiền thành công. Yêu cầu của bạn đang chờ xử lý.", (int)HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return BaseResponse<object>.Error($"Đã xảy ra lỗi: {ex.Message}", (int)HttpStatusCode.InternalServerError);
            }
        }
        public async Task<BaseResponse<TeacherBankAccountDto>> AddBankAccountAsync(Guid teacherId, CreateBankAccountDto dto)
        {
            // Resolve TeacherProfileId from incoming id (userId)
            var teacherProfile = await _unit.TeacherProfiles.FindAsync(t => t.UserId == teacherId);
            if (teacherProfile == null)
            {
                return BaseResponse<TeacherBankAccountDto>.Fail(null, "Không tìm thấy hồ sơ giáo viên.", (int)HttpStatusCode.NotFound);
            }
            var teacherProfileId = teacherProfile.TeacherId;

            //1. Kiểm tra xem tài khoản này đã tồn tại chưa cho giáo viên hiện tại
            var existing = (await _unit.TeacherBankAccounts
                .GetByConditionAsync(b => b.TeacherId == teacherProfileId && b.AccountNumber == dto.AccountNumber && b.BankName == dto.BankName))
                .FirstOrDefault();

            if (existing != null)
            {
                return BaseResponse<TeacherBankAccountDto>.Fail(null, "Tài khoản ngân hàng này đã tồn tại.", (int)HttpStatusCode.BadRequest);
            }

            //2. Kiểm tra đây có phải tài khoản đầu tiên của giáo viên không
            var allUserAccounts = await _unit.TeacherBankAccounts
                .GetByConditionAsync(b => b.TeacherId == teacherProfileId);

            var newAccount = new TeacherBankAccount
            {
                TeacherId = teacherProfileId,
                BankName = dto.BankName,
                BankBranch = dto.BankBranch,
                AccountNumber = dto.AccountNumber,
                AccountHolder = dto.AccountHolderName,
                CreatedAt = TimeHelper.GetVietnamTime(),
                // Tự động set làm default nếu đây là tài khoản đầu tiên
                IsDefault = !allUserAccounts.Any()
            };

            await _unit.TeacherBankAccounts.AddAsync(newAccount);
            await _unit.SaveChangesAsync();

            var resultDto = new TeacherBankAccountDto
            {
                BankAccountId = newAccount.BankAccountId,
                TeacherId = newAccount.TeacherId,
                BankName = newAccount.BankName,
                BankBranch = newAccount.BankBranch ?? string.Empty,
                AccountNumber = newAccount.AccountNumber,
                AccountHolderName = newAccount.AccountHolder,
                IsDefault = newAccount.IsDefault
            };

            return BaseResponse<TeacherBankAccountDto>.Success(resultDto, "Thêm tài khoản ngân hàng thành công.", (int)HttpStatusCode.Created);
        }
        public async Task<BaseResponse<IEnumerable<TeacherBankAccountDto>>> GetMyBankAccountsAsync(Guid teacherId)
        {
            // Resolve TeacherProfileId from incoming id (userId)
            var teacherProfile = await _unit.TeacherProfiles.FindAsync(t => t.UserId == teacherId);
            if (teacherProfile == null)
            {
                return BaseResponse<IEnumerable<TeacherBankAccountDto>>.Success(Enumerable.Empty<TeacherBankAccountDto>(), "Không tìm thấy hồ sơ giáo viên.", (int)HttpStatusCode.OK);
            }
            var teacherProfileId = teacherProfile.TeacherId;

            var accounts = await _unit.TeacherBankAccounts
                .GetByConditionAsync(b => b.TeacherId == teacherProfileId);


            var resultDtoList = accounts.Select(account => new TeacherBankAccountDto
            {
                BankAccountId = account.BankAccountId,
                TeacherId = account.TeacherId,
                BankName = account.BankName,
                BankBranch = account.BankBranch ?? string.Empty,
                AccountNumber = account.AccountNumber,
                AccountHolderName = account.AccountHolder,
                IsDefault = account.IsDefault
            })
            .OrderByDescending(dto => dto.IsDefault);

            return BaseResponse<IEnumerable<TeacherBankAccountDto>>.Success(
                resultDtoList,
                "Lấy danh sách tài khoản ngân hàng thành công.",
                (int)HttpStatusCode.OK
            );
        }
        public async Task<BaseResponse<PublicTeacherProfileDto>> GetPublicTeacherProfileAsync(Guid teacherId)
        {
            var profile = await _unit.TeacherProfiles.GetPublicProfileByIdAsync(teacherId);

            if (profile == null)
            {
                return BaseResponse<PublicTeacherProfileDto>.Fail(null, "Không tìm thấy hồ sơ giáo viên.", (int)HttpStatusCode.NotFound);
            }


            var publishedCourses = profile.Courses
                .Where(c => c.Status == CourseStatus.Published)
                .ToList();


            int totalCourses = publishedCourses.Count;


            var publishedCoursesDto = publishedCourses.Select(c => new TeacherCourseInfoDto
            {
                CourseId = c.CourseID,
                Title = c.Title,
                ImageUrl = c.ImageUrl,
                Price = c.Price,
                DiscountPrice = c.DiscountPrice,
                LearnerCount = c.LearnerCount,
                AverageRating = c.AverageRating,
                ReviewCount = c.ReviewCount
            }).ToList();


            int courseStudents = publishedCourses.Sum(c => c.LearnerCount);



            int totalStudents = courseStudents;


            double averageRating = profile.AverageRating;
            int totalReviews = profile.ReviewCount;


            var resultDto = new PublicTeacherProfileDto
            {
                TeacherId = profile.TeacherId,
                UserId = profile.UserId,
                FullName = profile.FullName,
                Avatar = profile.Avatar,
                Bio = profile.Bio,

                TotalCourses = totalCourses,
                TotalStudents = totalStudents,
                AverageRating = averageRating,
                TotalReviews = totalReviews,

                PublishedCourses = publishedCoursesDto
            };

            return BaseResponse<PublicTeacherProfileDto>.Success(resultDto, "Lấy hồ sơ giáo viên thành công.", (int)HttpStatusCode.OK);
        }
        public async Task<PagedResponse<IEnumerable<TeachingProgramResponse>>> GetTeachingProgramAsync(Guid userId, int pageNumber, int pageSize)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var teacher = await _unit.TeacherProfiles.Query()
                    .Include(t => t.TeacherProgramAssignments)
                        .ThenInclude(tpa => tpa.Program)
                    .Include(t => t.TeacherProgramAssignments)
                        .ThenInclude(tpa => tpa.Level)
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (teacher == null)
                {
                    return PagedResponse<IEnumerable<TeachingProgramResponse>>.Fail(
                        new object(),
                        "Teacher not found",
                        404
                    );
                }

                var assignmentsQuery = _unit.TeacherProgramAssignments.Query()
                    .Where(tpa => tpa.TeacherId == teacher.TeacherId && tpa.Status)
                    .Include(tpa => tpa.Program)
                    .Include(tpa => tpa.Level)
                    .AsQueryable();

                var totalItems = await assignmentsQuery.CountAsync();

                var assignments = await assignmentsQuery
                    .OrderBy(tpa => tpa.Program.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(tpa => new TeachingProgramResponse
                    {
                        ProgramId = tpa.ProgramId,
                        ProgramName = tpa.Program.Name,
                        LevelId = tpa.LevelId,
                        LevelName = tpa.Level.Name
                    })
                    .ToListAsync();

                if (!assignments.Any())
                {
                    return PagedResponse<IEnumerable<TeachingProgramResponse>>.Success(
                        new List<TeachingProgramResponse>(),
                        pageNumber,
                        pageSize,
                        0,
                        "No teaching programs found"
                    );
                }

                return PagedResponse<IEnumerable<TeachingProgramResponse>>.Success(
                    assignments,
                    pageNumber,
                    pageSize,
                    totalItems,
                    "Teaching programs retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<TeachingProgramResponse>>.Error(
                    $"An error occurred while retrieving teaching programs: {ex.Message}",
                    500
                );
            }
        }
        public async Task<PagedResponse<List<TeacherSearchResponse>>> SearchTeachersAsync(TeacherSearchRequest request)
        {
            try
            {
                var query = _unit.TeacherProfiles.Query()
                    .Include(t => t.User)
                    .Include(t => t.Language)
                    .Include(t => t.TeacherReviews)
                    .Include(t => t.ExerciseGradingAssignments)
                    .AsNoTracking();

                query = query.Where(t => t.Status == true);

                if (request.LanguageId.HasValue)
                {
                    query = query.Where(t => t.LanguageId == request.LanguageId.Value);
                }

                if (!string.IsNullOrEmpty(request.LanguageCode))
                {
                    query = query.Where(t => t.Language.LanguageCode == request.LanguageCode);
                }

                if (!string.IsNullOrEmpty(request.ProficiencyCode))
                {
                    query = query.Where(t => t.ProficiencyCode == request.ProficiencyCode);
                }

                if (request.MinProficiencyOrder.HasValue)
                {
                    query = query.Where(t => t.ProficiencyOrder >= request.MinProficiencyOrder.Value);
                }

                if (request.MaxProficiencyOrder.HasValue)
                {
                    query = query.Where(t => t.ProficiencyOrder <= request.MaxProficiencyOrder.Value);
                }

                if (request.MinRating.HasValue)
                {
                    query = query.Where(t => t.AverageRating >= request.MinRating.Value);
                }

                if (request.MaxRating.HasValue)
                {
                    query = query.Where(t => t.AverageRating <= request.MaxRating.Value);
                }

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.FullName.ToLower().Contains(searchTerm) ||
                        t.Email.ToLower().Contains(searchTerm) ||
                        t.Bio.ToLower().Contains(searchTerm) ||
                        t.Language.LanguageName.ToLower().Contains(searchTerm));
                }

                if (request.MinReviewCount.HasValue)
                {
                    query = query.Where(t => t.ReviewCount >= request.MinReviewCount.Value);
                }

                query = request.SortBy?.ToLower() switch
                {
                    "rating" => request.SortDescending == true
                        ? query.OrderByDescending(t => t.AverageRating)
                        : query.OrderBy(t => t.AverageRating),
                    "reviews" => request.SortDescending == true
                        ? query.OrderByDescending(t => t.ReviewCount)
                        : query.OrderBy(t => t.ReviewCount),
                    "proficiency" => request.SortDescending == true
                        ? query.OrderByDescending(t => t.ProficiencyOrder)
                        : query.OrderBy(t => t.ProficiencyOrder),
                    "name" => request.SortDescending == true
                        ? query.OrderByDescending(t => t.FullName)
                        : query.OrderBy(t => t.FullName),
                    _ => query.OrderByDescending(t => t.AverageRating)
                };

                var totalCount = await query.CountAsync();

                var teachers = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var response = teachers.Select(t => new TeacherSearchResponse
                {
                    TeacherId = t.TeacherId,
                    UserId = t.UserId,
                    FullName = t.FullName,
                    Email = t.Email,
                    PhoneNumber = t.PhoneNumber,
                    Bio = t.Bio,
                    Avatar = t.Avatar,
                    LanguageId = t.LanguageId,
                    LanguageName = t.Language?.LanguageName ?? string.Empty,
                    LanguageCode = t.Language?.LanguageCode ?? string.Empty,
                    ProficiencyCode = t.ProficiencyCode,
                    ProficiencyOrder = t.ProficiencyOrder,
                    AverageRating = t.AverageRating,
                    ReviewCount = t.ReviewCount,
                    MeetingUrl = t.MeetingUrl,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt.ToString("dd-MM-yyyy"),
                    TotalGradedAssignments = t.ExerciseGradingAssignments?
                        .Count(a => a.Status == GradingStatus.Returned) ?? 0,
                    ActiveAssignments = t.ExerciseGradingAssignments?
                        .Count(a => a.Status == GradingStatus.Assigned) ?? 0,
                }).ToList();

                return PagedResponse<List<TeacherSearchResponse>>.Success(
                    response,
                    request.Page,
                    request.PageSize,
                    totalCount,
                    "Teachers retrieved successfully");
            }
            catch (Exception ex)
            {
                return PagedResponse<List<TeacherSearchResponse>>.Error($"Error searching teachers: {ex.Message}");
            }
        }
    }
}










