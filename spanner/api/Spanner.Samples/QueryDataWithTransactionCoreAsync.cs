// Copyright 2020 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// [START spanner_read_only_transaction_core]

using Google.Cloud.Spanner.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class QueryDataWithTransactionCoreAsyncSample
{
    public class Album
    {
        public int SingerId { get; set; }
        public int AlbumId { get; set; }
        public string AlbumTitle { get; set; }
    }

    public async Task<List<Album>> QueryDataWithTransactionCoreAsync(string projectId, string instanceId, string databaseId)
    {
        string connectionString = $"Data Source=projects/{projectId}/instances/{instanceId}/databases/{databaseId}";

        var albums = new List<Album>();

        using var connection = new SpannerConnection(connectionString);
        await connection.OpenAsync();

        // Open a new read only transaction.
        using var transaction = await connection.BeginTransactionAsync(
            SpannerTransactionCreationOptions.ReadOnly,
            transactionOptions: null,
            cancellationToken: default);
        using var cmd = connection.CreateSelectCommand("SELECT SingerId, AlbumId, AlbumTitle FROM Albums");
        cmd.Transaction = transaction;

        // Read #1.
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                Console.WriteLine("SingerId : " + reader.GetFieldValue<string>("SingerId")
                    + " AlbumId : " + reader.GetFieldValue<string>("AlbumId")
                    + " AlbumTitle : " + reader.GetFieldValue<string>("AlbumTitle"));
            }
        }

        // Read #2. Even if changes occur in-between the reads,
        // the transaction ensures that Read #1 and Read #2
        // return the same data.
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                albums.Add(new Album
                {
                    AlbumId = reader.GetFieldValue<int>("AlbumId"),
                    SingerId = reader.GetFieldValue<int>("SingerId"),
                    AlbumTitle = reader.GetFieldValue<string>("AlbumTitle")
                });
            }
        }

        Console.WriteLine("Transaction complete.");
        return albums;
    }
}
// [END spanner_read_only_transaction_core]
