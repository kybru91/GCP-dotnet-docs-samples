/*
 * Copyright 2024 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Google.Cloud.SecretManager.V1;
using Xunit;

[Collection(nameof(RegionalSecretManagerFixture))]
public class RegionalQuickstartTests
{
    private readonly RegionalSecretManagerFixture _fixture;
    private readonly RegionalQuickstartSample _sample;

    public RegionalQuickstartTests(RegionalSecretManagerFixture fixture)
    {
        _fixture = fixture;
        _sample = new RegionalQuickstartSample();
    }

    [Fact]
    public void Runs()
    {
        // Get the secret name.
        SecretName secretName = SecretName.FromProjectLocationSecret(_fixture.ProjectId, _fixture.LocationId, _fixture.RandomId()); ;

        // Run the code sample.
        _sample.RegionalQuickstart(projectId: secretName.ProjectId, locationId: secretName.LocationId, secretId: secretName.SecretId);

        // Clean the created secret.
        _fixture.DeleteSecret(secretName);
    }
}
