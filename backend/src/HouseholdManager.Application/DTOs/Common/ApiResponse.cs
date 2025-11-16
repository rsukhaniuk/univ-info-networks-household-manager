using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.DTOs.Common
{
    /// <summary>
    /// Standard API response wrapper for consistent response format
    /// </summary>
    /// <typeparam name="T">Type of the data payload</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message (success or error message)
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// The actual data payload
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// List of validation errors (if any)
        /// </summary>
        public Dictionary<string, string[]>? Errors { get; set; }

        /// <summary>
        /// Timestamp of the response
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "Request completed successfully",
                Data = data
            };
        }

        /// <summary>
        /// Creates a successful response without data (for operations like Delete)
        /// </summary>
        public static ApiResponse<T> SuccessResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = default
            };
        }

        /// <summary>
        /// Creates an error response with a single error message
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }

        /// <summary>
        /// Creates an error response with validation errors
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, Dictionary<string, string[]> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                Data = default
            };
        }

        /// <summary>
        /// Creates a not found response
        /// </summary>
        public static ApiResponse<T> NotFoundResponse(string message = "Resource not found")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }

        /// <summary>
        /// Creates an unauthorized response
        /// </summary>
        public static ApiResponse<T> UnauthorizedResponse(string message = "Unauthorized access")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }

    /// <summary>
    /// Non-generic API response for operations without data payload
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message (success or error message)
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// List of validation errors (if any)
        /// </summary>
        public Dictionary<string, string[]>? Errors { get; set; }

        /// <summary>
        /// Timestamp of the response
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a successful response
        /// </summary>
        public static ApiResponse SuccessResponse(string message)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        /// <summary>
        /// Creates an error response
        /// </summary>
        public static ApiResponse ErrorResponse(string message)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message
            };
        }

        /// <summary>
        /// Creates an error response with validation errors
        /// </summary>
        public static ApiResponse ErrorResponse(string message, Dictionary<string, string[]> errors)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }
}
