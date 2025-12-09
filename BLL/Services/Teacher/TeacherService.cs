using BLL.IServices.Teacher;
using BLL.IServices.FirebaseService;
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
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text;

namespace BLL.Services.Teacher
{
    public class TeacherService : ITeacherService
    {
        private readonly IUnitOfWork _unit;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly ILogger<TeacherService> _logger;

        public TeacherService(
            IUnitOfWork unit,
            IFirebaseNotificationService firebaseNotificationService,
            ILogger<TeacherService> logger)
        {
            _unit = unit;
            _firebaseNotificationService = firebaseNotificationService;
            _logger = logger;
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

                // === GỬI THÔNG BÁO CHO ADMIN ===
                await SendPayoutNotificationToAdminsAsync(teacherProfile.FullName, requestDto.Amount);

                return BaseResponse<object>.Success(null, "Gửi yêu cầu rút tiền thành công. Yêu cầu của bạn đang chờ xử lý.", (int)HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return BaseResponse<object>.Error($"Đã xảy ra lỗi: {ex.Message}", (int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Helper: Gửi Web Push notification cho Admin(s) khi có yêu cầu rút tiền mới
        /// </summary>
        private async Task SendPayoutNotificationToAdminsAsync(string teacherName, decimal amount)
        {
            try
            {
                // Lấy danh sách Admin
                var adminRole = await _unit.Roles.FindAsync(r => r.Name == "Admin");
                if (adminRole == null) return;

                var admins = await _unit.UserRoles.GetQuery()
                    .Where(ur => ur.RoleID == adminRole.RoleID)
                    .Select(ur => ur.User)
                    .ToListAsync();

                var adminTokens = admins
                    .Where(a => a != null && !string.IsNullOrEmpty(a.FcmToken))
                    .Select(a => a!.FcmToken!)
                    .ToList();

                if (adminTokens.Any())
                {
                    await _firebaseNotificationService.SendNewPayoutRequestToAdminAsync(
                        adminTokens,
                        teacherName,
                        amount
                    );

                    _logger.LogInformation("[FCM-Web] ✅ Sent payout request notification to {Count} admin(s)",
                        adminTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM-Web] ❌ Failed to send payout notification to admins");
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
                        ProgramAssignmentId = tpa.ProgramAssignmentId,
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
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
        public async Task<PagedResponse<IEnumerable<TeacherClassDto>>> SearchClassesAsync(Guid teacherId, string? keyword, string? status, DateTime? from, DateTime? to, Guid? programId, int page, int pageSize)
        {
            // Build query with filters that can be translated to SQL
            var query = _unit.TeacherClasses.Query()
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t!.TeacherProfile)
                .Where(c => c.TeacherID == teacherId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ClassStatus>(status, true, out var statusEnum))
                    query = query.Where(c => c.Status == statusEnum);
            }
            if (from.HasValue) query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(c => c.CreatedAt <= to.Value);
            if (programId.HasValue) query = query.Where(c => c.ProgramId == programId.Value);

            // Execute query and get list
            var list = await query.ToListAsync();

            // Log total before keyword filter
            var totalBeforeKeyword = list.Count;

            // Apply keyword filter on client side
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var keywordNoDiacritics = RemoveDiacritics(keyword.Trim()).ToLower();

                // Debug log
                System.Diagnostics.Debug.WriteLine($"[SEARCH] Keyword: '{keyword}' -> Normalized: '{keywordNoDiacritics}'");
                System.Diagnostics.Debug.WriteLine($"[SEARCH] Total classes before keyword filter: {totalBeforeKeyword}");

                var filtered = new List<TeacherClass>();
                foreach (var c in list)
                {
                    var titleOriginal = c.Title ?? string.Empty;
                    var titleTrimmed = titleOriginal.Trim();
                    var titleNorm = RemoveDiacritics(titleTrimmed).ToLower();

                    System.Diagnostics.Debug.WriteLine($"[SEARCH] Class '{titleOriginal}' -> Normalized: '{titleNorm}' -> Contains '{keywordNoDiacritics}': {titleNorm.Contains(keywordNoDiacritics)}");

                    if (titleNorm.Contains(keywordNoDiacritics))
                    {
                        filtered.Add(c);
                    }
                }
                list = filtered;

                System.Diagnostics.Debug.WriteLine($"[SEARCH] Total after keyword filter: {list.Count}");
            }

            var total = list.Count;
            var items = list.OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new TeacherClassDto
                {
                    ClassID = c.ClassID,
                    Title = c.Title,
                    Description = c.Description ?? string.Empty,
                    LanguageID = c.LanguageID,
                    LanguageName = c.Language?.LanguageName,
                    // Lấy FullName từ TeacherProfile, fallback UserName
                    TeacherName = c.Teacher?.TeacherProfile?.FullName 
                                  ?? c.Teacher?.UserName 
                                  ?? "Unknown Teacher",
                    StartDateTime = c.StartDateTime,
                    EndDateTime = c.EndDateTime,
                    MinStudents = c.MinStudents,
                    Capacity = c.Capacity,
                    PricePerStudent = c.PricePerStudent,
                    GoogleMeetLink = c.GoogleMeetLink,
                    Status = c.Status.ToString(),
                    CurrentEnrollments = c.CurrentEnrollments,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();

            return PagedResponse<IEnumerable<TeacherClassDto>>.Success(items, page, pageSize, total);
        }
        public async Task<BaseResponse<TeacherProfileWithWalletResponse>> GetTeacherProfileWithWalletAsync(Guid userId)
        {
            var profile = await GetTeacherProfileAsync(userId);
            var teacher = await _unit.TeacherProfiles.GetByUserIdAsync(userId);
            var wallet = teacher != null ? await _unit.Wallets.GetByTeacherIdAsync(teacher.TeacherId) : null;
            var walletDto = wallet != null ? new TeacherWalletDto
            {
                WalletId = wallet.WalletId,
                TotalBalance = wallet.TotalBalance,
                AvailableBalance = wallet.AvailableBalance,
                HoldBalance = wallet.HoldBalance,
                Currency = Enum.GetName(typeof(DAL.Type.CurrencyType), wallet.Currency) ?? "VND"
            } : null;
            var result = new TeacherProfileWithWalletResponse
            {
                Profile = profile.Data,
                Wallet = walletDto
            };
            return BaseResponse<TeacherProfileWithWalletResponse>.Success(result);
        }
        public async Task<BaseResponse<IEnumerable<TeacherProfileResponse>>> GetAllTeachersAsync()
        {
            var teachers = await _unit.TeacherProfiles.Query().ToListAsync();
            var languageDict = (await _unit.Languages.GetAllAsync()).ToDictionary(l => l.LanguageID, l => l.LanguageName);
            var result = teachers.Select(t => new TeacherProfileResponse
            {
                TeacherId = t.TeacherId,
                Language = languageDict.TryGetValue(t.LanguageId, out var lang) ? lang : null,
                FullName = t.FullName,
                DateOfBirth = t.BirthDate.ToString("dd-MM-yyyy"),
                Bio = t.Bio,
                Avatar = t.Avatar,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                ProficiencyCode = t.ProficiencyCode,
                AverageRating = t.AverageRating,
                ReviewCount = t.ReviewCount,
                MeetingUrl = t.MeetingUrl
            }).ToList();
            return BaseResponse<IEnumerable<TeacherProfileResponse>>.Success(result);
        }
        public async Task<PagedResponse<IEnumerable<TeacherClassDto>>> PublicSearchClassesAsync(Guid? languageId, Guid? teacherId, Guid? programId, string? keyword, string? status, DateTime? from, DateTime? to, int page, int pageSize)
        {
            // Build query with public filters and include navigation properties
            IQueryable<TeacherClass> query = _unit.TeacherClasses.Query()
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t!.TeacherProfile)
                .Include(c => c.Enrollments); // Include enrollments for CurrentEnrollments calculation

            if (languageId.HasValue) query = query.Where(c => c.LanguageID == languageId.Value);

            // Support searching by:
            // 1. User.UserID (TeacherID in TeacherClass)
            // 2. TeacherProfile.TeacherId (from TeacherProfile table)
            if (teacherId.HasValue)
            {
                // Get all TeacherProfiles with matching TeacherId or UserId
                var teacherProfiles = await _unit.TeacherProfiles.Query()
                    .Where(tp => tp.TeacherId == teacherId.Value || tp.UserId == teacherId.Value)
                    .Select(tp => tp.UserId)
                    .ToListAsync();

                if (teacherProfiles.Any())
                {
                    // Filter by User.UserID that matches TeacherProfile.UserId
                    query = query.Where(c => teacherProfiles.Contains(c.TeacherID));
                }
                else
                {
                    // Fallback: direct match by TeacherID (User.UserID)
                    query = query.Where(c => c.TeacherID == teacherId.Value);
                }
            }

            if (programId.HasValue) query = query.Where(c => c.ProgramId == programId.Value);
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ClassStatus>(status, true, out var statusEnum))
                    query = query.Where(c => c.Status == statusEnum);
            }
            if (from.HasValue) query = query.Where(c => c.StartDateTime >= from.Value);
            if (to.HasValue) query = query.Where(c => c.EndDateTime <= to.Value);

            // Execute query and get list
            var list = await query.ToListAsync();

            // Apply keyword filter on client side (support Vietnamese with/without diacritics)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var keywordNoDiacritics = RemoveDiacritics(keyword.Trim()).ToLower();
                list = list.Where(c =>
                    RemoveDiacritics((c.Title ?? string.Empty).Trim()).ToLower().Contains(keywordNoDiacritics)
                ).ToList();
            }

            var total = list.Count;

            // Get enrollment counts for all classes in the page
            var classIds = list.Skip((page - 1) * pageSize).Take(pageSize).Select(c => c.ClassID).ToList();
            var enrollmentCounts = await _unit.ClassEnrollments.Query()
                .Where(e => classIds.Contains(e.ClassID) && e.Status == DAL.Models.EnrollmentStatus.Paid)
                .GroupBy(e => e.ClassID)
                .Select(g => new { ClassID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassID, x => x.Count);

            var items = list.OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new TeacherClassDto
                {
                    ClassID = c.ClassID,
                    Title = c.Title ?? string.Empty,
                    Description = c.Description ?? string.Empty,
                    LanguageID = c.LanguageID,
                    LanguageName = c.Language?.LanguageName,
                    // Lấy FullName từ TeacherProfile, fallback UserName
                    TeacherName = c.Teacher?.TeacherProfile?.FullName 
                                  ?? c.Teacher?.UserName 
                                  ?? "Unknown Teacher",
                    StartDateTime = c.StartDateTime,
                    EndDateTime = c.EndDateTime,
                    MinStudents = c.MinStudents,
                    Capacity = c.Capacity,
                    PricePerStudent = c.PricePerStudent,
                    GoogleMeetLink = c.GoogleMeetLink,
                    Status = c.Status.ToString(),
                    CurrentEnrollments = enrollmentCounts.TryGetValue(c.ClassID, out var count) ? count : 0,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();

            return PagedResponse<IEnumerable<TeacherClassDto>>.Success(items, page, pageSize, total);
        }
        public async Task<PagedResponse<IEnumerable<PayoutRequestDto>>> GetMyPayoutRequestsAsync(Guid teacherId, string? status, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var teacherProfile = await _unit.TeacherProfiles.FindAsync(t => t.UserId == teacherId);
            if (teacherProfile == null) return PagedResponse<IEnumerable<PayoutRequestDto>>.Success(new List<PayoutRequestDto>(), page, pageSize, 0);
            var query = _unit.PayoutRequests.Query().Where(p => p.TeacherId == teacherProfile.TeacherId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<PayoutStatus>(status, true, out var statusEnum))
                    query = query.Where(p => p.PayoutStatus == statusEnum);
            }
            if (from.HasValue) query = query.Where(p => p.RequestedAt >= from.Value);
            if (to.HasValue) query = query.Where(p => p.RequestedAt <= to.Value);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(p => p.RequestedAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new PayoutRequestDto
                {
                    PayoutRequestId = p.PayoutRequestId,
                    Amount = p.Amount,
                    Status = p.PayoutStatus.ToString(),
                    RequestedAt = p.RequestedAt,
                    ProcessedAt = p.UpdatedAt,
                    ApprovedAt = p.ApprovedAt,
                    Note = p.Note,
                    BankName = p.BankAccount != null ? p.BankAccount.BankName : null,
                    AccountNumber = p.BankAccount != null ? p.BankAccount.AccountNumber : null
                }).ToListAsync();
            return PagedResponse<IEnumerable<PayoutRequestDto>>.Success(items, page, pageSize, total);
        }
        public async Task<BaseResponse<TeacherDashboardDto>> GetTeacherDashboardAsync(Guid teacherId, DateTime? from, DateTime? to, string? status, Guid? programId)
        {
            var teacherProfile = await _unit.TeacherProfiles.FindAsync(t => t.UserId == teacherId);
            if (teacherProfile == null) return BaseResponse<TeacherDashboardDto>.Success(new TeacherDashboardDto());
            var classQuery = _unit.TeacherClasses.Query()
                .Include(c => c.Program)
                .Where(c => c.TeacherID == teacherId);
            if (from.HasValue) classQuery = classQuery.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue) classQuery = classQuery.Where(c => c.CreatedAt <= to.Value);
            if (programId.HasValue) classQuery = classQuery.Where(c => c.ProgramId == programId.Value);
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ClassStatus>(status, true, out var statusEnum))
                    classQuery = classQuery.Where(c => c.Status == statusEnum);
            }
            var classList = await classQuery.ToListAsync();
            var classIds = classList.Select(c => c.ClassID).ToList();
            var enrollments = await _unit.ClassEnrollments.Query()
                .Where(e => classIds.Contains(e.ClassID) && e.Status == DAL.Models.EnrollmentStatus.Paid)
                .ToListAsync();
            var classSummaries = classList.Select(c => new ClassSummaryDto
            {
                ClassID = c.ClassID,
                Title = c.Title ?? string.Empty,
                Status = c.Status.ToString(),
                StartDateTime = c.StartDateTime,
                EndDateTime = c.EndDateTime,
                StudentCount = enrollments.Count(e => e.ClassID == c.ClassID),
                Revenue = enrollments.Where(e => e.ClassID == c.ClassID).Sum(e => e.AmountPaid),
                ProgramId = c.ProgramId,
                ProgramName = c.Program?.Name ?? string.Empty
            }).ToList();
            var payoutQuery = _unit.PayoutRequests.Query().Where(p => p.TeacherId == teacherProfile.TeacherId);
            var payoutList = await payoutQuery.OrderByDescending(p => p.RequestedAt).Take(10).ToListAsync();
            var payoutSummaries = payoutList.Select(p => new PayoutSummaryDto
            {
                PayoutRequestId = p.PayoutRequestId,
                Amount = p.Amount,
                Status = p.PayoutStatus.ToString(),
                RequestedAt = p.RequestedAt,
                ProcessedAt = p.UpdatedAt
            }).ToList();
            var programStats = classList.GroupBy(c => new { c.ProgramId, ProgramName = c.Program?.Name ?? "" })
                .Select(g => new ProgramStatsDto
                {
                    ProgramId = g.Key.ProgramId,
                    ProgramName = g.Key.ProgramName,
                    ClassCount = g.Count(),
                    StudentCount = g.Sum(c => enrollments.Count(e => e.ClassID == c.ClassID)),
                    Revenue = g.Sum(c => enrollments.Where(e => e.ClassID == c.ClassID).Sum(e => e.AmountPaid))
                }).ToList();
            var periodStats = classList.GroupBy(c => c.CreatedAt.ToString("yyyy-MM"))
                .Select(g => new PeriodStatsDto
                {
                    Period = g.Key,
                    ClassCount = g.Count(),
                    StudentCount = g.Sum(c => enrollments.Count(e => e.ClassID == c.ClassID)),
                    Revenue = g.Sum(c => enrollments.Where(e => e.ClassID == c.ClassID).Sum(e => e.AmountPaid))
                }).OrderByDescending(p => p.Period).ToList();
            var totalClasses = classList.Count;
            var activeClasses = classList.Count(c => c.Status == ClassStatus.Published || c.Status == ClassStatus.InProgress);
            var finishedClasses = classList.Count(c => c.Status == ClassStatus.Finished || c.Status == ClassStatus.Completed_Paid);
            var totalStudents = enrollments.Count;
            var totalRevenue = enrollments.Sum(e => e.AmountPaid);
            var totalPayout = payoutList.Where(p => p.PayoutStatus == PayoutStatus.Completed).Sum(p => p.Amount);
            var pendingPayouts = payoutList.Count(p => p.PayoutStatus == PayoutStatus.Pending);
            var completedPayouts = payoutList.Count(p => p.PayoutStatus == PayoutStatus.Completed);
            var cancelledPayouts = payoutList.Count(p => p.PayoutStatus == PayoutStatus.Cancelled);
            var dashboard = new TeacherDashboardDto
            {
                TotalClasses = totalClasses,
                ActiveClasses = activeClasses,
                FinishedClasses = finishedClasses,
                TotalStudents = totalStudents,
                TotalRevenue = totalRevenue,
                TotalPayout = totalPayout,
                PendingPayouts = pendingPayouts,
                CompletedPayouts = completedPayouts,
                CancelledPayouts = cancelledPayouts,
                Classes = classSummaries,
                Payouts = payoutSummaries,
                ProgramStats = programStats,
                PeriodStats = periodStats
            };
            return BaseResponse<TeacherDashboardDto>.Success(dashboard);
        }
    }
}
