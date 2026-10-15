namespace WebHealth.Server.Models;
using System;
public record PhaseResult(string PhaseName, bool IsSuccess, long DurationMs, string? ErrorMessage);
    //public string PhaseName { get; set; } = string.Empty;
    //public bool IsSuccess { get; set; } = false;
    //public long DurationMs { get; set; } = 0;
    //public string? ErrorMessage { get; set; } = null;
