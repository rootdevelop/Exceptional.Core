using System;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace Exceptional.Core.Stores
{
    /// <summary>
    /// An <see cref="ErrorStore"/> implementation that uses SQL Server as its backing store. 
    /// </summary>
    public sealed class SQLErrorStore
    {
        private readonly string _connectionString;
        private readonly string _applicationName;


        /// <summary>
        /// Creates a new instance of <see cref="SQLErrorStore"/> with the specified connection string.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="connectionString">The database connection string to use</param>
        public SQLErrorStore(string applicationName, string connectionString)
        {
            _applicationName = applicationName;

            if (connectionString.IsNullOrEmpty()) throw new ArgumentOutOfRangeException(nameof(connectionString), "Connection string must be specified when using a SQL error store");
            _connectionString = connectionString;
        }

        /// <summary>
        /// Logs the error to SQL
        /// If the rollup conditions are met, then the matching error will have a DuplicateCount += @DuplicateCount (usually 1, unless in retry) rather than a distinct new row for the error
        /// </summary>
        /// <param name="error">The error to log</param>
        public void LogError(Error error)
        {
            using (var c = GetConnection())
            {
              
                    var queryParams = new DynamicParameters(new
                        {
                            error.DuplicateCount,
                            error.ErrorHash,
                            ApplicationName = _applicationName,
                            minDate = DateTime.UtcNow.AddMinutes(-30)
                        });
                    queryParams.Add("@newGUID", dbType: DbType.Guid, direction: ParameterDirection.Output);
                    var count = c.Execute(@"
                                            Update Exceptions 
                                               Set DuplicateCount = DuplicateCount + @DuplicateCount,
                                                   @newGUID = GUID
                                             Where Id In (Select Top 1 Id
                                                            From Exceptions 
                                                           Where ErrorHash = @ErrorHash
                                                             And ApplicationName = @ApplicationName
                                                             And DeletionDate Is Null
                                                             And CreationDate >= @minDate)", queryParams);
                    // if we found an error that's a duplicate, jump out
                    if (count > 0)
                    {
                        error.GUID = queryParams.Get<Guid>("@newGUID");
                        return;
                    }
                

                error.FullJson = error.ToJson();

                c.Execute(@"
                            Insert Into Exceptions (GUID, ApplicationName, MachineName, CreationDate, Type, IsProtected, Host, Url, HTTPMethod, IPAddress, Source, Message, Detail, StatusCode, SQL, FullJson, ErrorHash, DuplicateCount)
                            Values (@GUID, @ApplicationName, @MachineName, @CreationDate, @Type, @IsProtected, @Host, @Url, @HTTPMethod, @IPAddress, @Source, @Message, @Detail, @StatusCode, @SQL, @FullJson, @ErrorHash, @DuplicateCount)",
                    new {
                            error.GUID,
                            ApplicationName = _applicationName.Truncate(50),
                            MachineName = error.MachineName.Truncate(50),
                            error.CreationDate,
                            Type = error.Type.Truncate(100),
                            error.IsProtected,
                            Host = error.Host.Truncate(100),
                            Url = error.Url.Truncate(500),
                            HTTPMethod = error.HTTPMethod.Truncate(10), // this feels silly, but you never know when someone will up and go crazy with HTTP 1.2!
                            error.IPAddress,
                            Source = error.Source.Truncate(100),
                            Message = error.Message.Truncate(1000),
                            error.Detail,
                            error.StatusCode,
                            error.Sql,
                            error.FullJson,
                            error.ErrorHash,
                            error.DuplicateCount
                        });
            }
        }

        
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}