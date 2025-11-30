using BLL.IServices.TeacherReview;
using Common.DTO.ApiResponse;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.TeacherReview.Request;
using Common.DTO.TeacherReview.Response;
using DAL.Helpers;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.TeacherReview
{
    public class TeacherReviewService : ITeacherReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        public TeacherReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<BaseResponse<TeacherReviewResponse>> CreateTeacherReviewAsync(Guid userId, Guid teacherId, TeacherReviewRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponse<TeacherReviewResponse>.Fail(null, "Access denied.", 403);
                }

                var teacher = await _unitOfWork.TeacherProfiles.GetByIdAsync(teacherId);
                if (teacher == null)
                {
                    return BaseResponse<TeacherReviewResponse>.Fail(null, "Teacher not found.", 404);
                }

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId && l.LanguageId == teacher.LanguageId);
                if (learner == null)
                {
                    return BaseResponse<TeacherReviewResponse>.Fail(null, "You must be a learner of this teacher's language to review them.", 403);
                }

                var hasStudied = await _unitOfWork.Enrollments.Query()
                    .Include(e => e.Course)
                    .AnyAsync(e => e.LearnerId == learner.LearnerLanguageId && e.Course.TeacherId == teacherId);

                if (!hasStudied)
                {
                    return BaseResponse<TeacherReviewResponse>.Fail(null, "You must have enrolled in at least one course by this teacher to review them.", 403);
                }

                var existingReview = await _unitOfWork.TeacherReviews.FindAsync(x => x.TeacherProfileId == teacherId && x.LearnerId == learner.LearnerLanguageId);
                if (existingReview != null)
                {
                    return BaseResponse<TeacherReviewResponse>.Fail(MapToResponse(existingReview, user.FullName ?? user.UserName, user.Avatar, teacher.FullName), "You have already reviewed this teacher.", 400);
                }

                var newReview = new DAL.Models.TeacherReview
                {
                    TeacherReviewId = Guid.NewGuid(),
                    LearnerId = learner.LearnerLanguageId,
                    TeacherProfileId = teacherId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };

                await _unitOfWork.TeacherReviews.AddAsync(newReview);
                await _unitOfWork.SaveChangesAsync();

                await UpdateTeacherRatingAsync(teacherId);

                return BaseResponse<TeacherReviewResponse>.Success(MapToResponse(newReview, user.FullName ?? user.UserName, user.Avatar, teacher.FullName), "Teacher review created successfully.", 201);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating teacher review: {ex.Message}");
                return BaseResponse<TeacherReviewResponse>.Error("An error occurred while creating the teacher review.");
            }
        }

        public async Task<BaseResponse<bool>> DeleteTeacherReviewAsync(Guid userId, Guid teacherReviewId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponse<bool>.Fail(false, "Access denied.", 403);
                }

                var review = await _unitOfWork.TeacherReviews
                    .Query()
                    .Include(r => r.Learner)
                    .FirstOrDefaultAsync(r => r.TeacherReviewId == teacherReviewId);

                if (review == null)
                {
                    return BaseResponse<bool>.Fail(false, "Review not found.", 404);
                }

                if (review.Learner.UserId != userId)
                {
                    return BaseResponse<bool>.Fail(false, "You are not authorized to delete this review.", 403);
                }

                var teacherId = review.TeacherProfileId;

                _unitOfWork.TeacherReviews.Remove(review);
                await _unitOfWork.SaveChangesAsync();

                await UpdateTeacherRatingAsync(teacherId);

                return BaseResponse<bool>.Success(true, "Review deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting teacher review: {ex.Message}");
                return BaseResponse<bool>.Error("An error occurred while deleting the review.");
            }
        }

        public async Task<PagedResponse<List<TeacherReviewResponse>>> GetTeacherReviewsByTeacherIdAsync(Guid teacherId, PaginationParams @params)
        {
            try
            {
                var query = _unitOfWork.TeacherReviews
                    .Query()
                    .Where(x => x.TeacherProfileId == teacherId)
                    .Include(x => x.Learner)
                        .ThenInclude(l => l.User)
                    .Include(x => x.TeacherProfile)
                    .AsNoTracking();

                if (@params.Rating.HasValue && @params.Rating > 0)
                {
                    query = query.Where(x => x.Rating == @params.Rating.Value);
                }

                if (!string.IsNullOrEmpty(@params.Sort))
                {
                    switch (@params.Sort.ToLower())
                    {
                        case "rating_desc":
                            query = query.OrderByDescending(x => x.Rating).ThenByDescending(x => x.CreatedAt);
                            break;
                        case "rating_asc":
                            query = query.OrderBy(x => x.Rating).ThenByDescending(x => x.CreatedAt);
                            break;
                        case "oldest":
                            query = query.OrderBy(x => x.CreatedAt);
                            break;
                        default:
                            query = query.OrderByDescending(x => x.CreatedAt);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.CreatedAt);
                }

                var totalItems = await query.CountAsync();

                var items = await query
                    .Skip((@params.PageNumber - 1) * @params.PageSize)
                    .Take(@params.PageSize)
                    .Select(x => new TeacherReviewResponse
                    {
                        TeacherReviewId = x.TeacherReviewId,
                        LearnerId = x.Learner.UserId,
                        LearnerName = x.Learner.User.FullName ?? x.Learner.User.UserName,
                        LearnerAvatar = x.Learner.User.Avatar,
                        TeacherId = x.TeacherProfileId,
                        TeacherName = x.TeacherProfile.FullName,
                        Rating = x.Rating,
                        Comment = x.Comment,
                        CreatedAt = x.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                        ModifiedDate = x.UpdatedAt.ToString("dd-MM-yyyy HH:mm")
                    })
                    .ToListAsync();

                return PagedResponse<List<TeacherReviewResponse>>.Success(
                    items,
                    @params.PageNumber,
                    @params.PageSize,
                    totalItems
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting teacher reviews: {ex.Message}");
                return PagedResponse<List<TeacherReviewResponse>>.Error("An error occurred while retrieving reviews.");
            }
        }

        public async Task<BaseResponse<TeacherReviewResponse>> UpdateTeacherReviewAsync(Guid userId, Guid teacherId, TeacherReviewRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) return BaseResponse<TeacherReviewResponse>.Fail(null, "User not found.", 404);

                var teacher = await _unitOfWork.TeacherProfiles.GetByIdAsync(teacherId);
                if (teacher == null) return BaseResponse<TeacherReviewResponse>.Fail(null, "Teacher not found.", 404);

                var learner = await _unitOfWork.LearnerLanguages
                    .Query()
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.LanguageId == teacher.LanguageId);

                if (learner == null) return BaseResponse<TeacherReviewResponse>.Fail(null, "Learner profile not found.", 403);

                var review = await _unitOfWork.TeacherReviews
                    .Query()
                    .FirstOrDefaultAsync(x => x.TeacherProfileId == teacherId && x.LearnerId == learner.LearnerLanguageId);

                if (review == null)
                {
                    return BaseResponse<TeacherReviewResponse>.Fail(null, "Review not found.", 404);
                }

                if (string.IsNullOrWhiteSpace(request.Comment))
                    return BaseResponse<TeacherReviewResponse>.Fail(null, "Comment cannot be empty.", 400);

                bool sameRating = review.Rating == request.Rating;
                bool sameComment = review.Comment?.Trim() == request.Comment.Trim();

                if (sameRating && sameComment)
                    return BaseResponse<TeacherReviewResponse>.Fail(null, "Nothing has changed.", 400);

                review.Rating = request.Rating;
                review.Comment = request.Comment.Trim();
                review.UpdatedAt = TimeHelper.GetVietnamTime();

                _unitOfWork.TeacherReviews.Update(review);
                await _unitOfWork.SaveChangesAsync();

                await UpdateTeacherRatingAsync(teacherId);

                return BaseResponse<TeacherReviewResponse>.Success(MapToResponse(review, user.FullName ?? user.UserName, user.Avatar, teacher.FullName), "Review updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating teacher review: {ex.Message}");
                return BaseResponse<TeacherReviewResponse>.Error("An error occurred while updating the review.");
            }
        }
        #region Helper Methods
        private async Task UpdateTeacherRatingAsync(Guid teacherId)
        {
            var stats = await _unitOfWork.TeacherReviews.Query()
                .Where(x => x.TeacherProfileId == teacherId)
                .GroupBy(x => x.TeacherProfileId)
                .Select(g => new
                {
                    Count = g.Count(),
                    Average = g.Average(r => r.Rating)
                })
                .FirstOrDefaultAsync();

            int count = stats?.Count ?? 0;
            double average = stats?.Average ?? 0;

            var teacher = await _unitOfWork.TeacherProfiles.GetByIdAsync(teacherId);
            if (teacher != null)
            {
                teacher.ReviewCount = count;
                teacher.AverageRating = Math.Round(average, 1);

                _unitOfWork.TeacherProfiles.Update(teacher);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        private TeacherReviewResponse MapToResponse(DAL.Models.TeacherReview review, string learnerName, string learnerAvatar, string teacherName)
        {
            return new TeacherReviewResponse
            {
                TeacherReviewId = review.TeacherReviewId,
                LearnerId = review.LearnerId,
                LearnerName = learnerName,
                LearnerAvatar = learnerAvatar,
                TeacherId = review.TeacherProfileId,
                TeacherName = teacherName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                ModifiedDate = review.UpdatedAt.ToString("dd-MM-yyyy HH:mm")
            };
        }
        #endregion
    }
}
