using System;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Exceptional.Core
{
    /// <summary>
    /// Represents a logical application error (as opposed to the actual exception it may be representing).
    /// </summary>
    public class Error
    {

        /// <summary>
        /// Unique identifier for this error, generated on the server it came from
        /// </summary>
        public Guid GUID { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class
        /// from a given <see cref="Exception"/> instance and 
        /// <see cref="HttpContext"/> instance representing the HTTP 
        /// context during the exception.
        /// </summary>
        public Error(Exception e, HttpContext context = null)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            Exception = e;
            var baseException = e.GetBaseException();
            
            GUID = Guid.NewGuid();
            MachineName = Environment.MachineName;
            Type = baseException.GetType().FullName;
            Message = baseException.Message;
            Source = baseException.Source;
            Detail = e.ToString();
            CreationDate = DateTime.UtcNow;
            DuplicateCount = 1;

            if (context != null)
            {
                IPAddress = context.Connection.RemoteIpAddress.ToString();
                HTTPMethod = context.Request.Method;
                StatusCode = 500; // TODO find a way to get the statuscode
                Host = context.Request.Host.Host;
                Url = context.Request.Path.ToString();
            }

            ErrorHash = GetHash();
        }


        /// <summary>
        /// Gets a unique-enough hash of this error.  Stored as a quick comparison mechanism to rollup duplicate errors.
        /// </summary>
        /// <returns>"Unique" hash for this error</returns>
        public int? GetHash()
        {
            if (!Detail.HasValue()) return null;

            var result = Detail.GetHashCode();
            if (RollupPerServer && MachineName.HasValue())
                result = (result * 397)^ MachineName.GetHashCode();

            return result;
        }

        /// <summary>
        /// Reflects if the error is protected from deletion
        /// </summary>
        public bool IsProtected { get; set; }

        /// <summary>
        /// Gets the <see cref="Exception"/> instance used to create this error
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets the name of the application that threw this exception
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets the hostname of where the exception occured
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Get the type of error
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets the source of this error
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets the exception message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets the detail/stack trace of this error
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// The hash that describes this error
        /// </summary>
        public int? ErrorHash { get; set; }

        /// <summary>
        /// Gets the time in UTC that the error occured
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets the HTTP Status code associated with the request
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// The number of newer Errors that have been discarded because they match this Error and fall within the configured 
        /// "IgnoreSimilarExceptionsThreshold" TimeSpan value.
        /// </summary>
        public int? DuplicateCount { get; set; }

        /// <summary>
        /// This flag is to indicate that there were no matches of this error in when added to the queue or store.
        /// </summary>

        public bool IsDuplicate { get; set; }


        /// <summary>
        /// The URL host of the request causing this error
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The URL path of the request causing this error
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The HTTP Method causing this error, e.g. GET or POST
        /// </summary>
        public string HTTPMethod { get; set; }

        /// <summary>
        /// The IPAddress of the request causing this error
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// Json populated from database stored, deserialized after if needed
        /// </summary>

        public string FullJson { get; set; }

        /// <summary>
        /// Whether to roll up errors per-server. E.g. should an identical error happening on 2 separate servers be a DuplicateCount++, or 2 separate errors.
        /// </summary>

        public bool RollupPerServer { get; set; }

        /// <summary>
        /// Returns the value of the <see cref="Message"/> property.
        /// </summary>
        public override string ToString() => Message;
        
        /// <summary>
        /// Gets a JSON representation for this error
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

    }
}