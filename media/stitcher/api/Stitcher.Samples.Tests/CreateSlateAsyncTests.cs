/*
 * Copyright 2022 Google LLC
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

using System.Threading.Tasks;
using Xunit;

namespace Stitcher.Samples.Tests
{
    [Collection(nameof(StitcherFixture))]
    public class CreateSlateAsyncTest
    {
        private StitcherFixture _fixture;
        private readonly CreateSlateSample _createSample;

        private string _slateId;

        public CreateSlateAsyncTest(StitcherFixture fixture)
        {
            _fixture = fixture;
            _createSample = new CreateSlateSample();
            _slateId = $"{_fixture.SlateIdPrefix}-{StitcherFixture.GetRandomId()}-{StitcherFixture.GetTimestampId()}";
        }

        [Fact]
        public async Task CreatesSlateAsync()
        {
            // Run the sample code.
            _fixture.SlateIds.Add(_slateId);
            var result = await _createSample.CreateSlateAsync(
                _fixture.ProjectId, _fixture.LocationId, _slateId, _fixture.TestSlateUri);

            Assert.Equal(_slateId, result.SlateName.SlateId);
        }
    }
}
