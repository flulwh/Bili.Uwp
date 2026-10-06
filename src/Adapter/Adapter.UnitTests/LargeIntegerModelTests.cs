// Copyright (c) Richasy. All rights reserved.

using Bili.Models.BiliBili;
using Newtonsoft.Json;
using Xunit;

namespace Bili.Adapter.UnitTests
{
    public class LargeIntegerModelTests
    {
        [Fact]
        public void DeserializeSubPartitionWithLargeOffsetId()
        {
            const long offsetId = 117381187835804;
            var partition = JsonConvert.DeserializeObject<SubPartitionDefault>($"{{\"cbottom\":{offsetId}}}");

            Assert.Equal(offsetId, partition.BottomOffsetId);
        }

        [Fact]
        public void DeserializePgcModuleWithLargeViewCount()
        {
            const long viewCount = 7428825379;
            var module = JsonConvert.DeserializeObject<PgcModule>(
                $"{{\"items\":[{{\"stat\":{{\"view\":{viewCount}}}}}]}}");

            Assert.Equal(viewCount, module.Items[0].Stat.ViewCount);
        }
    }
}
