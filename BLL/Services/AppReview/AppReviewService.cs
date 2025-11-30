using BLL.IServices.AppReview;
using Common.DTO.ApiResponse;
using Common.DTO.AppReview.Request;
using Common.DTO.AppReview.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.AppReview
{
    public class AppReviewService : IAppReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        public AppReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<BaseResponse<AppReviewResponse>> CreateAppReviewAsync(Guid userId, AppReviewRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) return BaseResponse<AppReviewResponse>.Fail(null, "User not found.", 404);

                var existingReview = await _unitOfWork.Reviews.Query()
                    .FirstOrDefaultAsync(r => r.UserId == userId);

                if (existingReview != null)
                {
                    return BaseResponse<AppReviewResponse>.Fail(MapToResponse(existingReview, user), "You have already reviewed the application.", 400);
                }

                var newReview = new DAL.Models.Review
                {
                    ReviewId = Guid.NewGuid(),
                    UserId = userId,
                    Rating = request.Rating,
                    Content = request.Content,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };

                await _unitOfWork.Reviews.AddAsync(newReview);
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<AppReviewResponse>.Success(MapToResponse(newReview, user), "Review submitted successfully.", 201);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating app review: {ex.Message}");
                return BaseResponse<AppReviewResponse>.Error("An error occurred while submitting your review.");
            }
        }

        public async Task<BaseResponse<bool>> DeleteAppReviewAsync(Guid userId, Guid reviewId)
        {
            try
            {
                var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
                if (review == null) return BaseResponse<bool>.Fail(false, "Review not found.", 404);

                if (review.UserId != userId)
                    return BaseResponse<bool>.Fail(false, "You are not authorized to delete this review.", 403);

                _unitOfWork.Reviews.Remove(review);
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<bool>.Success(true, "Review deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting app review: {ex.Message}");
                return BaseResponse<bool>.Error("An error occurred while deleting the review.");
            }
        }
        public async Task<PagedResponse<List<AppReviewResponse>>> GetAllAppReviewsAsync(PaginationParams @params)
        {
            try
            {
                var query = _unitOfWork.Reviews.Query()
                    .Include(r => r.User)
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
                    .Select(r => new AppReviewResponse
                    {
                        ReviewId = r.ReviewId,
                        UserId = r.UserId,
                        UserFullName = r.User.FullName ?? r.User.UserName,
                        UserAvatar = r.User.Avatar,
                        Rating = r.Rating,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                        UpdatedAt = r.UpdatedAt.ToString("dd-MM-yyyy HH:mm")
                    })
                    .ToListAsync();

                return PagedResponse<List<AppReviewResponse>>.Success(items, @params.PageNumber, @params.PageSize, totalItems);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting app reviews: {ex.Message}");
                return PagedResponse<List<AppReviewResponse>>.Error("An error occurred while retrieving reviews.");
            }
        }

        public async Task<BaseResponse<AppReviewResponse>> GetMyReviewAsync(Guid userId)
        {
            try
            {
                var review = await _unitOfWork.Reviews.Query()
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.UserId == userId);

                if (review == null)
                    return BaseResponse<AppReviewResponse>.Fail(null, "You haven't reviewed the app yet.", 404);

                return BaseResponse<AppReviewResponse>.Success(MapToResponse(review, review.User), "Review retrieved successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<AppReviewResponse>.Error($"Error retrieving review: {ex.Message}");
            }
        }

        public async Task<BaseResponse<AppReviewResponse>> UpdateAppReviewAsync(Guid userId, Guid reviewId, AppReviewRequest request)
        {
            try
            {
                var review = await _unitOfWork.Reviews.Query()
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

                if (review == null) return BaseResponse<AppReviewResponse>.Fail(null, "Review not found.", 404);

                if (review.UserId != userId)
                    return BaseResponse<AppReviewResponse>.Fail(null, "You are not authorized to update this review.", 403);

                if (review.Rating == request.Rating && review.Content == request.Content)
                {
                    return BaseResponse<AppReviewResponse>.Fail(null, "Nothing has changed.", 400);
                }

                review.Rating = request.Rating;
                review.Content = request.Content;
                review.UpdatedAt = TimeHelper.GetVietnamTime();

                _unitOfWork.Reviews.Update(review);
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<AppReviewResponse>.Success(MapToResponse(review, review.User), "Review updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating app review: {ex.Message}");
                return BaseResponse<AppReviewResponse>.Error("An error occurred while updating the review.");
            }
        }

        #region Helper Methods
        private AppReviewResponse MapToResponse(DAL.Models.Review review, DAL.Models.User user)
        {
            return new AppReviewResponse
            {
                ReviewId = review.ReviewId,
                UserId = review.UserId,
                UserFullName = user.FullName ?? user.UserName,
                UserAvatar = user.Avatar,
                Rating = review.Rating,
                Content = review.Content,
                CreatedAt = review.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                UpdatedAt = review.UpdatedAt.ToString("dd-MM-yyyy HH:mm")
            };
        }
        #endregion
    }
}
