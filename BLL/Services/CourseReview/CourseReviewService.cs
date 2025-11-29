using BLL.IServices.Course;
using Common.DTO.ApiResponse;
using Common.DTO.CourseReview.Request;
using Common.DTO.CourseReview.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.CourseReview
{
    public class CourseReviewService : ICourseReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CourseReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<BaseResponse<CourseReviewResponse>> CreateCourseReviewAsync(Guid userId, Guid courseId, CourseReviewRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponse<CourseReviewResponse>.Fail(new object(), "Access denied.", 403);
                }

                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course == null)
                {
                    return BaseResponse<CourseReviewResponse>.Fail(new object(), "Course not found.", 404);
                }

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId && l.LanguageId == course.LanguageId);
                if (learner == null)
                {
                    return BaseResponse<CourseReviewResponse>.Fail(new object(), "You must be a learner of this course's language to review it.", 403);
                }

                var enrollment = await _unitOfWork.Enrollments.FindAsync(e => e.LearnerId == learner.LearnerLanguageId && e.CourseId == course.CourseID);
                if (enrollment == null)
                {
                    return BaseResponse<CourseReviewResponse>.Fail(new object(), "You must be enrolled in this course to review it.", 403);
                }

                CourseReviewResponse courseReviewResponse = new();

                var existingReview = await _unitOfWork.CourseReviews.FindAsync(x => x.CourseId == courseId && x.LearnerId == learner.LearnerLanguageId);

                if (existingReview != null)
                {
                    return BaseResponse<CourseReviewResponse>.Fail(MapToResponse(existingReview, user.FullName ?? user.UserName, user.Avatar, course.Title), "You have already reviewed this course.", 400);
                }

                var newReview = new DAL.Models.CourseReview
                {
                    CourseReviewId = Guid.NewGuid(),
                    LearnerId = learner.LearnerLanguageId,
                    CourseId = courseId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    ModifiedDate = TimeHelper.GetVietnamTime()
                };

                await _unitOfWork.CourseReviews.AddAsync(newReview);
                await _unitOfWork.SaveChangesAsync();

                await UpdateCourseRatingAsync(courseId);

                return BaseResponse<CourseReviewResponse>.Success(MapToResponse(newReview, user.FullName ?? user.UserName, user.Avatar, course.Title), "Course review created successfully.", 201);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating course review: {ex.Message}");
                return BaseResponse<CourseReviewResponse>.Error("An error occurred while creating the course review.");
            }
        }
        public async Task<BaseResponse<bool>> DeleteCourseReviewAsync(Guid userId, Guid courseReviewId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponse<bool>.Fail(new object(), "Access denied.", 403);
                }

                var review = await _unitOfWork.CourseReviews
                    .Query()
                    .Include(r => r.Learner)
                    .FirstOrDefaultAsync(r => r.CourseReviewId == courseReviewId);

                if (review == null)
                {
                    return BaseResponse<bool>.Fail(false, "Review not found.", 404);
                }

                if (review.Learner.UserId != userId)
                {
                    return BaseResponse<bool>.Fail(false, "You are not authorized to delete this review.", 403);
                }

                var courseId = review.CourseId;

                _unitOfWork.CourseReviews.Remove(review);
                await _unitOfWork.SaveChangesAsync();

                await UpdateCourseRatingAsync(courseId);

                return BaseResponse<bool>.Success(true, "Review deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting review: {ex.Message}");
                return BaseResponse<bool>.Error("An error occurred while deleting the review.");
            }
        }
        public async Task<PagedResponse<List<CourseReviewResponse>>> GetCourseReviewsByCourseIdAsync(Guid courseId, PaginationParams @params)
        {
            try
            {
                var query = _unitOfWork.CourseReviews
                    .Query()
                    .Where(x => x.CourseId == courseId)
                    .Include(x => x.Learner)
                        .ThenInclude(l => l.User)
                    .Include(x => x.Course)
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
                            query = query.OrderByDescending(x => x.Rating)
                                         .ThenByDescending(x => x.CreatedAt);
                            break;

                        case "rating_asc":
                            query = query.OrderBy(x => x.Rating)
                                         .ThenByDescending(x => x.CreatedAt);
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
                    .Select(x => new CourseReviewResponse
                    {
                        CourseReviewId = x.CourseReviewId,
                        LearnerId = x.Learner.UserId,
                        LearnerName = x.Learner.User.FullName ?? x.Learner.User.UserName,
                        LearnerAvatar = x.Learner.User.Avatar,
                        CourseId = x.CourseId,
                        CourseTitle = x.Course.Title,
                        Rating = x.Rating,
                        Comment = x.Comment,
                        CreatedAt = x.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                        ModifiedDate = x.ModifiedDate.ToString("dd-MM-yyyy HH:mm")
                    })
                    .ToListAsync();

                return PagedResponse<List<CourseReviewResponse>>.Success(
                    items,
                    @params.PageNumber,
                    @params.PageSize,
                    totalItems
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting reviews: {ex.Message}");
                return PagedResponse<List<CourseReviewResponse>>.Error("An error occurred while retrieving reviews.");
            }
        }
        public async Task<BaseResponse<CourseReviewResponse>> UpdateCourseReviewAsync(Guid userId, Guid courseId, CourseReviewRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) return BaseResponse<CourseReviewResponse>.Fail(null, "User not found.", 404);

                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course == null) return BaseResponse<CourseReviewResponse>.Fail(null, "Course not found.", 404);

                var learner = await _unitOfWork.LearnerLanguages
                    .Query()
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.LanguageId == course.LanguageId);

                if (learner == null) return BaseResponse<CourseReviewResponse>.Fail(null, "Learner profile not found.", 403);

                var review = await _unitOfWork.CourseReviews
                    .Query()
                    .FirstOrDefaultAsync(x => x.CourseId == courseId && x.LearnerId == learner.LearnerLanguageId);

                if (review == null)
                {
                    return BaseResponse<CourseReviewResponse>.Fail(null, "Review not found.", 404);
                }

                if (string.IsNullOrWhiteSpace(request.Comment))
                    return BaseResponse<CourseReviewResponse>.Fail(null, "Comment cannot be empty.", 400);

                bool sameRating = review.Rating == request.Rating;
                bool sameComment = review.Comment?.Trim() == request.Comment.Trim();

                if (sameRating && sameComment)
                    return BaseResponse<CourseReviewResponse>.Fail(null, "Nothing has changed.", 400);

                review.Rating = request.Rating;
                review.Comment = request.Comment.Trim();
                review.ModifiedDate = TimeHelper.GetVietnamTime();

                _unitOfWork.CourseReviews.Update(review);
                await _unitOfWork.SaveChangesAsync();

                await UpdateCourseRatingAsync(courseId);

                return BaseResponse<CourseReviewResponse>.Success(MapToResponse(review, user.FullName ?? user.UserName, user.Avatar, course.Title), "Review updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating review: {ex.Message}");
                return BaseResponse<CourseReviewResponse>.Error("An error occurred while updating the review.");
            }
        }
        #region Helper Methods
        private async Task UpdateCourseRatingAsync(Guid courseId)
        {
            var stats = await _unitOfWork.CourseReviews.Query()
                .Where(x => x.CourseId == courseId)
                .GroupBy(x => x.CourseId)
                .Select(g => new
                {
                    Count = g.Count(),
                    Average = g.Average(r => r.Rating)
                })
                .FirstOrDefaultAsync();

            int count = stats?.Count ?? 0;
            double average = stats?.Average ?? 0;

            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course != null)
            {
                course.ReviewCount = count;
                course.AverageRating = Math.Round(average, 1);
                _unitOfWork.Courses.Update(course);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        private CourseReviewResponse MapToResponse(DAL.Models.CourseReview review, string learnerName, string learnerAvatar, string courseTitle)
        {
            return new CourseReviewResponse
            {
                CourseReviewId = review.CourseReviewId,
                LearnerId = review.LearnerId,
                LearnerName = learnerName,
                LearnerAvatar = learnerAvatar,
                CourseId = review.CourseId,
                CourseTitle = courseTitle,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                ModifiedDate = review.ModifiedDate.ToString("dd-MM-yyyy HH:mm")
            };
        }
        #endregion
    }
}
