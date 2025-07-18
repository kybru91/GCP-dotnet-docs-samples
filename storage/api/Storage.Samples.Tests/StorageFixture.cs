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

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Iam.V1;
using Google.Cloud.PubSub.V1;
using Google.Cloud.Storage.V1;
using GoogleCloudSamples;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Xunit;
using Xunit.Sdk;
using static Google.Apis.Storage.v1.Data.Bucket;

[CollectionDefinition(nameof(StorageFixture))]
public class StorageFixture : IDisposable, ICollectionFixture<StorageFixture>
{
    public string ProjectId { get; }
    public IList<string> TempBucketNames { get; } = new List<string>();
    public Dictionary<string, List<string>> TempBucketFiles { get; } = new Dictionary<string, List<string>>();
    public Dictionary<string, Dictionary<string, List<long>>> TempBucketArchivedFiles { get; }
        = new Dictionary<string, Dictionary<string, List<long>>>();
    public string BucketNameGeneric { get; } = Guid.NewGuid().ToString();
    public string BucketNameRegional { get; } = Guid.NewGuid().ToString();

    public string BucketNameHns { get; } = Guid.NewGuid().ToString();
    public string TestLocation { get; } = "us-west1";
    public string FileName { get; } = "Hello.txt";
    public string FilePath { get; } = "Resources/Hello.txt";
    public string KmsKeyRing { get; } = Environment.GetEnvironmentVariable("STORAGE_KMS_KEYRING");
    public string KmsKeyName { get; } = Environment.GetEnvironmentVariable("STORAGE_KMS_KEYNAME");
    public string KmsKeyLocation { get; } = "us-west1";
    public string ServiceAccountEmail { get; } = "gcs-iam-acl-test@dotnet-docs-samples-tests.iam.gserviceaccount.com";
    public List<TopicName> TempTopicNames { get; } = new List<TopicName>();
    public StorageClient Client { get; }

    public RetryRobot HmacChangesPropagated { get; } = new RetryRobot
    {
        ShouldRetry = ex => ex is XunitException ||
            (ex is GoogleApiException gex &&
                (gex.HttpStatusCode == HttpStatusCode.NotFound ||
                gex.HttpStatusCode == HttpStatusCode.BadRequest ||
                gex.HttpStatusCode == HttpStatusCode.ServiceUnavailable))
    };

    public StorageFixture()
    {
        ProjectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
        if (string.IsNullOrWhiteSpace(ProjectId))
        {
            throw new Exception("You need to set the Environment variable 'GOOGLE_PROJECT_ID' with your Google Cloud Project's project id.");
        }
        Client = StorageClient.Create();
        // create simple bucket
        CreateBucket(BucketNameGeneric);

        // create regional bucket
        CreateRegionalBucketSample createRegionalBucketSample = new CreateRegionalBucketSample();
        createRegionalBucketSample.CreateRegionalBucket(ProjectId, BucketNameRegional, TestLocation, StorageClasses.Regional);
        SleepAfterBucketCreateUpdateDelete();
        TempBucketNames.Add(BucketNameRegional);

        // create hns bucket
        CreateBucketWithHierarchicalNamespaceEnabledSample createBucketWithHierarchicalNamespaceEnabledSample =
            new CreateBucketWithHierarchicalNamespaceEnabledSample();
        createBucketWithHierarchicalNamespaceEnabledSample.CreateBucketWithHierarchicalNamespace(ProjectId,
            BucketNameHns);
        SleepAfterBucketCreateUpdateDelete();
        TempBucketNames.Add(BucketNameHns);

        //upload file to BucketName
        UploadFileSample uploadFileSample = new UploadFileSample();
        uploadFileSample.UploadFile(BucketNameGeneric, FilePath, FileName);

        Collect(FileName);
    }

    public void Dispose()
    {
        DeleteFileSample deleteFileSample = new DeleteFileSample();
        DeleteFileArchivedGenerationSample deleteFileArchivedGenerationSample = new DeleteFileArchivedGenerationSample();
        foreach (var bucket in TempBucketFiles)
        {
            foreach (var file in bucket.Value)
            {
                try
                {
                    deleteFileSample.DeleteFile(bucket.Key, file);
                }
                catch (Exception)
                {
                    // Do nothing, we delete on a best effort basis.
                }
            }
        }

        foreach (var bucket in TempBucketArchivedFiles)
        {
            foreach (var file in bucket.Value)
            {
                foreach (var version in file.Value)
                {
                    try
                    {
                        deleteFileArchivedGenerationSample.DeleteFileArchivedGeneration(bucket.Key, file.Key, version);
                    }
                    catch (Exception)
                    {
                        // Do nothing, we delete on a best effort basis.
                    }
                }
            }
        }

        foreach (var bucketName in TempBucketNames)
        {
            try
            {
                Client.DeleteBucket(bucketName, new DeleteBucketOptions { DeleteObjects = true });
                SleepAfterBucketCreateUpdateDelete();
            }
            catch (Exception)
            {
                // Do nothing, we delete on a best effort basis.
            }
        }
        foreach (TopicName topicName in TempTopicNames)
        {
            try
            {
                PublisherServiceApiClient publisher = PublisherServiceApiClient.Create();
                publisher.DeleteTopic(topicName);
            }
            catch (Exception)
            {
                // Do nothing, we are deleting on a best effort basis.
            }
        }
    }

    /// <summary>
    /// Add an object to delete at the end of the test.
    /// </summary>
    /// <returns>The objectName.</returns>
    private string Collect(string bucketName, string objectName)
    {
        if (!TempBucketFiles.TryGetValue(bucketName, out List<string> objectNames))
        {
            objectNames = TempBucketFiles[bucketName] = new List<string>();
        }
        objectNames.Add(objectName);
        return objectName;
    }

    /// <summary>
    /// Add an object to delete at the end of the test.
    /// </summary>
    /// <returns>The objectName.</returns>
    public string Collect(string objectName) => Collect(BucketNameGeneric, objectName);

    /// <summary>
    /// Add a object located in a regional bucket to delete
    /// at the end of the test.
    /// </summary>
    /// <returns>The regional objectName.</returns>
    public string CollectRegionalObject(string objectName) => Collect(BucketNameRegional, objectName);

    /// <summary>
    /// Add a object located in a hns bucket to delete
    /// at the end of the test.
    /// </summary>
    /// <returns>The objectName.</returns>
    public string CollectHnsObject(string objectName) => Collect(BucketNameHns, objectName);

    public void DeleteHmacKey(string accessId, bool isActive)
    {
        int retries = 10;
        DeactivateHmacKeySample deactivateHmacKeySample = new DeactivateHmacKeySample();
        DeleteHmacKeySample deleteHmacKeySample = new DeleteHmacKeySample();

        do
        {
            try
            {
                if (isActive)
                {
                    deactivateHmacKeySample.DeactivateHmacKey(ProjectId, accessId);
                    isActive = false;
                }
                deleteHmacKeySample.DeleteHmacKey(ProjectId, accessId);
                return;
            }
            catch when (--retries > 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            catch
            {
                return;
                // This is still cleanup, it's on a best effort basis.
            }
        } while (true);
    }

    public void CreateBucket(string bucketName, string location = null, AutoclassData autoclassData = null)
    {
        StorageClient storageClient = StorageClient.Create();
        storageClient.CreateBucket(ProjectId, new Bucket { Name = bucketName, Location = location, Autoclass = autoclassData });
        SleepAfterBucketCreateUpdateDelete();
        TempBucketNames.Add(bucketName);
    }

    internal Bucket CreateBucket(string name, bool multiVersion, bool softDelete = false, bool registerForDeletion = true)
    {
        var bucket = Client.CreateBucket(ProjectId,
            new Bucket
            {
                Name = name,
                Versioning = new Bucket.VersioningData { Enabled = multiVersion },
                // The minimum allowed for soft delete is 7 days.
                SoftDeletePolicy = softDelete ? new Bucket.SoftDeletePolicyData { RetentionDurationSeconds = (int) TimeSpan.FromDays(7).TotalSeconds } : null,
            });
        SleepAfterBucketCreateUpdateDelete();
        if (registerForDeletion)
        {
            TempBucketNames.Add(name);
        }
        return bucket;
    }

    internal string GenerateBucketName() => Guid.NewGuid().ToString();

    /// <summary>
    /// Generate the name of the object.
    /// </summary>
    /// <returns>The objectName.</returns>
    internal string GenerateName() => Guid.NewGuid().ToString();

    /// <summary>
    /// Generate the content of the object.
    /// </summary>
    /// <returns>The objectContent.</returns>
    internal string GenerateContent() => Guid.NewGuid().ToString();

    /// <summary>
    /// Generates a new globally unique identifier (GUID).
    /// </summary>
    /// <returns>A new randomly generated GUID as string.</returns>
    internal string GenerateGuid() => Guid.NewGuid().ToString();

    /// <summary>
    /// Bucket creation/update/deletion is rate-limited. To avoid making the tests flaky, we sleep after each operation.
    /// </summary>
    internal void SleepAfterBucketCreateUpdateDelete() => Thread.Sleep(2000);

    internal string GetServiceAccountEmail()
    {
        var cred = GoogleCredential.GetApplicationDefault().UnderlyingCredential;
        switch (cred)
        {
            case ServiceAccountCredential sac:
                return sac.Id;
            // TODO: We may well need to handle ComputeCredential for Kokoro.
            default:
                throw new InvalidOperationException($"Unable to retrieve service account email address for credential type {cred.GetType()}");
        }
    }

    public void CollectArchivedFiles(string bucketName, string objectName, long? version)
    {
        if (!TempBucketArchivedFiles.TryGetValue(bucketName, out Dictionary<string, List<long>> objectNames))
        {
            objectNames = TempBucketArchivedFiles[bucketName] = new Dictionary<string, List<long>>();
        }

        if (!objectNames.TryGetValue(objectName, out List<long> versions))
        {
            versions = objectNames[objectName] = new List<long>();
        }
        versions.Add(version.Value);
    }

    public Topic CreateTopic(string topicId)
    {
        PublisherServiceApiClient publisherClient = PublisherServiceApiClient.Create();
        TopicName topicName = new TopicName(ProjectId, topicId);
        Topic topic = publisherClient.CreateTopic(topicName);
        TempTopicNames.Add(topicName);

        var policy = new Google.Cloud.Iam.V1.Policy();
        policy.AddRoleMember("roles/pubsub.publisher", "allUsers");
        publisherClient.IAMPolicyClient.SetIamPolicy(new SetIamPolicyRequest
        {
            ResourceAsResourceName = topicName,
            Policy = policy
        });

        return topic;
    }
}
