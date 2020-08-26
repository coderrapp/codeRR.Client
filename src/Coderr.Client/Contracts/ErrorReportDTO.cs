﻿using System;
using System.Collections.Generic;

namespace Coderr.Client.Contracts
{
    /// <summary>
    ///     DTO used to transfer the report from the applications to the server.
    /// </summary>
    public class ErrorReportDTO
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorReportDTO" /> class.
        /// </summary>
        /// <param name="reportId">Unique identifier for this error report.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="contextCollections">The context collections.</param>
        public ErrorReportDTO(string reportId, ExceptionDTO exception, ContextCollectionDTO[] contextCollections)
        {
            if (reportId == null) throw new ArgumentNullException(nameof(reportId));
            if (reportId.Contains(" ") || reportId.Length > 30)
                throw new ArgumentException(
                    $"reportId must be 30 or less characters and should be alphanumeric only. Your id '{reportId}' is {reportId.Length} chars.");

            ContextCollections = contextCollections ?? throw new ArgumentNullException(nameof(contextCollections));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            ReportId = reportId;
            ReportVersion = "1.0";
            CreatedAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorReportDTO" /> class.
        /// </summary>
        protected ErrorReportDTO()
        {
        }

        /// <summary>
        ///     A collection of context information such as HTTP request information or computer hardware info.
        /// </summary>
        public ContextCollectionDTO[] ContextCollections { get; set; }

        /// <summary>
        ///     To get exact date
        /// </summary>
        public DateTime CreatedAtUtc { get; }


        /// <summary>
        ///     Exception which was caught.
        /// </summary>
        public ExceptionDTO Exception { get; }

        /// <summary>
        ///     For backwards compatibility
        /// </summary>
        [Obsolete("Do not use")]
        protected string IncidentId
        {
            get => null;
            set
            {
                if (string.IsNullOrEmpty(ReportId))
                    ReportId = value;
            }
        }

        /// <summary>
        ///     Gets report id (unique identifier used in communication with the customer to identify this error)
        /// </summary>
        public string ReportId { get; private set; }

        /// <summary>
        ///     Version of the report API
        /// </summary>
        /// <example>
        ///     1.0
        /// </example>
        public string ReportVersion { get; }

        /// <summary>
        /// Application environment (like "Test" and "Production")
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// Name of collection to show per default in the UI.
        /// </summary>
        public string HighlightCollection { get; set; }

        /// <summary>
        /// The 100 last log entries before the exception was detected (can be null).
        /// </summary>
        public LogEntryDto[] LogEntries { get; set; }

        /// <summary>
        ///     Add an collection to the model
        /// </summary>
        /// <param name="collection">Collection of contextual information which can be used to aid in solving the error.</param>
        /// <exception cref="System.ArgumentNullException">collection</exception>
        public void Add(ContextCollectionDTO collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            var col = new List<ContextCollectionDTO>(ContextCollections) {collection};
            ContextCollections = col.ToArray();
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{ReportId} ({Exception.Message})";
        }
    }
}