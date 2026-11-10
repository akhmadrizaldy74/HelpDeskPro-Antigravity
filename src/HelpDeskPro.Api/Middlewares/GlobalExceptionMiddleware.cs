namespace HelpDeskPro.Api.Middlewares;

using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using HelpDeskPro.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                (ProblemDetails)CreateValidationProblemDetails(validationEx, traceId, context)),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                CreateProblemDetails(
                    StatusCodes.Status404NotFound,
                    "Data tidak ditemukan.",
                    notFoundEx.Message,
                    traceId,
                    context)),

            KeyNotFoundException keyNotFoundEx => (
                StatusCodes.Status404NotFound,
                CreateProblemDetails(
                    StatusCodes.Status404NotFound,
                    "Data tidak ditemukan.",
                    keyNotFoundEx.Message,
                    traceId,
                    context)),

            BusinessRuleException businessRuleEx => (
                StatusCodes.Status422UnprocessableEntity,
                CreateProblemDetails(
                    StatusCodes.Status422UnprocessableEntity,
                    "Pelanggaran aturan bisnis.",
                    businessRuleEx.Message,
                    traceId,
                    context)),

            ForbiddenAccessException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                CreateProblemDetails(
                    StatusCodes.Status403Forbidden,
                    "Akses ditolak.",
                    forbiddenEx.Message,
                    traceId,
                    context)),

            UnauthorizedAccessException unauthorizedEx => (
                StatusCodes.Status403Forbidden,
                CreateProblemDetails(
                    StatusCodes.Status403Forbidden,
                    "Akses ditolak.",
                    unauthorizedEx.Message,
                    traceId,
                    context)),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                CreateProblemDetails(
                    StatusCodes.Status409Conflict,
                    "Konflik pembaruan data.",
                    "Tiket sudah diubah pengguna lain, muat ulang halaman.",
                    traceId,
                    context)),

            _ => (
                StatusCodes.Status500InternalServerError,
                CreateProblemDetails(
                    StatusCodes.Status500InternalServerError,
                    "Terjadi kesalahan pada server.",
                    "Terjadi kesalahan internal pada sistem.",
                    traceId,
                    context))
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}. TraceId: {TraceId}", exception.Message, traceId);
        }
        else
        {
            _logger.LogWarning("Handled exception occurred: {Message}. TraceId: {TraceId}", exception.Message, traceId);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), _jsonSerializerOptions));
    }

    private static ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        string traceId,
        HttpContext context)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        return problemDetails;
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(
        ValidationException validationException,
        string traceId,
        HttpContext context)
    {
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Terjadi kesalahan validasi.",
            Detail = "Satu atau lebih kesalahan validasi terjadi pada data yang dikirimkan.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        return problemDetails;
    }
}
