using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.ContentTesting.Reports
{
    using Sitecore.Data;
    using Sitecore.ContentTesting.Reports;

    public class ContentTestPerformanceFactory : Sitecore.ContentTesting.Reports.ContentTestPerformanceFactory
    {
        public override IPersonalizationPerformance GetPersonalizationPerformanceInItem(ID contentItemId, ID testId)
        {
            return new Sitecore.Support.ContentTesting.Reports.PersonalizationPerformanceInItem(contentItemId, testId);
        }
    }
}

namespace Sitecore.Support.ContentTesting.Reports
{
    using Sitecore.Analytics.Reporting;
    using Sitecore.ContentTesting.Analytics.Reporting;
    using Sitecore.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sitecore.ContentTesting.Reports;
    using Sitecore.Diagnostics;
    using Sitecore.Support.ContentTesting.Analytics.Reporting;

    public class PersonalizationPerformanceInItem : SitecorePersonalizationPerformance
    {
        // Fields
        private ReachForItemQuery _reachForItemQuery;
        private SupportRulesetExposureQuery _rulesetExposureQuery;

        // Methods
        public PersonalizationPerformanceInItem(ID contentItemId, ID testId)
            : base(contentItemId, testId)
        {
        }

        public PersonalizationPerformanceInItem(ID contentItemId, ID testId, ReportDataProviderBase reportDataProvider)
            : base(contentItemId, testId, reportDataProvider)
        {
        }

        public override double? GetNotPersonalizedRuleValue(Guid ruleSetId, Guid ruleId)
        {
            if (base.TestId.Guid == Guid.Empty)
            {
                return null;
            }
            return base.GetNotPersonalizedRuleValue(ruleSetId, ruleId);
        }

        public override double? GetPersonalizedRuleValue(Guid ruleSetId, Guid ruleId)
        {
            if (base.TestId.Guid == Guid.Empty)
            {
                return null;
            }
            return base.GetPersonalizedRuleValue(ruleSetId, ruleId);
        }

        public override long GetReach()
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime startDate = this.GetStartDate(utcNow);
            if (this._reachForItemQuery == null)
            {
                this._reachForItemQuery = new ReachForItemQuery(base.ContentItemId, startDate, utcNow, base.ReportDataProvider);
                this._reachForItemQuery.Execute();
            }
            return this._reachForItemQuery.Visitors;
        }

        public override int GetReachRate(Guid ruleSetId, Guid ruleId)
        {
            if ((this._rulesetExposureQuery == null) || (this._rulesetExposureQuery.RulesetId != ruleSetId))
            {
                this._rulesetExposureQuery = this.GetRulesetExposure(ruleSetId);
            }
            //return this._rulesetExposureQuery.GetExposureIntegerPercentage(ruleId);
            return this.GetIntegerPercentage<Guid>(this._rulesetExposureQuery.TotalVisitors, this._rulesetExposureQuery._visitors.ToList<KeyValuePair<Guid, long>>(), ruleId);
        }

        internal int GetIntegerPercentage<TKey>(long total, List<KeyValuePair<TKey, long>> values, TKey key)
        {
            if (values != null)
            {
                int num = 100;
                for (int i = 0; i < values.Count; i++)
                {
                    KeyValuePair<TKey, long> pair = values[i];
                    int rate = this.GetRate(pair.Value, total);
                    num -= rate;
                    if (i == (values.Count - 1))
                    {
                        rate += num;
                    }
                    if (pair.Key.Equals(key))
                    {
                        return rate;
                    }
                }
            }
            return 0;
        }

        internal virtual int GetRate(long value, long totalValue)
        {
            if (totalValue == 0L)
            {
                return 0;
            }
            double a = (((double)value) / ((double)totalValue)) * 100.0;
            return (int)Math.Round(a);
        }



        public override double? GetRuleEffect(Guid ruleSetId, Guid ruleId)
        {
            Assert.ArgumentNotNull(ruleSetId, "ruleSetId");
            Assert.ArgumentNotNull(ruleId, "ruleId");
            if (base.TestId.Guid == Guid.Empty)
            {
                return null;
            }
            return base.GetRuleEffect(ruleSetId, ruleId);
        }

        public override double? GetRuleEffect(double? notPersonalizedValue, double? personalizedValue)
        {
            if (base.TestId.Guid == Guid.Empty)
            {
                return null;
            }
            return base.GetRuleEffect(notPersonalizedValue, personalizedValue);
        }

        public override long GetRuleReach(Guid ruleSetId, Guid ruleId)
        {
            if ((this._rulesetExposureQuery == null) || (this._rulesetExposureQuery.RulesetId != ruleSetId))
            {
                this._rulesetExposureQuery = this.GetRulesetExposure(ruleSetId);
            }
            return this._rulesetExposureQuery.GetExposure(ruleId);
        }

        protected virtual SupportRulesetExposureQuery GetRulesetExposure(Guid rulesetId)
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime startDate = this.GetStartDate(utcNow);
            SupportRulesetExposureQuery query = new SupportRulesetExposureQuery(base.ContentItemId, rulesetId, base.ReportDataProvider)
            {
                ReportStart = startDate,
                ReportEnd = utcNow
            };
            query.Execute();
            return query;
        }

        public override long GetRuleSetReach(Guid ruleSetId)
        {
            if ((this._rulesetExposureQuery == null) || (this._rulesetExposureQuery.RulesetId != ruleSetId))
            {
                this._rulesetExposureQuery = this.GetRulesetExposure(ruleSetId);
            }
            return this._rulesetExposureQuery.TotalVisitors;
        }
    }


}

namespace Sitecore.Support.ContentTesting.Analytics.Reporting
{
    using Sitecore.Analytics.Reporting;
    using Sitecore.Data;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Sitecore.ContentTesting.Analytics.Reporting;

    public class SupportRulesetExposureQuery : RulesetExposureQuery
    {
        public readonly Dictionary<Guid, long> _visitors;

        public override void Execute()
        {
            this._visitors.Clear();
            this.TotalVisitors = 0L;
            Dictionary<string, object> parameters = new Dictionary<string, object> {
            {
                "@StartDate",
                base.ReportStart
            },
            {
                "@EndDate",
                base.ReportEnd
            },
            {
                "@ItemId",
                this.ItemId
            },
            {
                "@RuleSetId",
                this.RulesetId
            }
        };
            foreach (DataRow row in this.ExecuteQuery(parameters).Rows)
            {
                Guid key = (Guid)row["RuleId"];
                long num = (long)row["Visitors"];
                if (!this._visitors.ContainsKey(key))
                {
                    this._visitors.Add(key, num);
                }
                else
                {
                    this._visitors[key] += num;
                }
                this.TotalVisitors += num;
            }
        }

        public new long TotalVisitors { get; private set; }

        public SupportRulesetExposureQuery(ID itemId, Guid rulesetId, ReportDataProviderBase reportProvider = null) : base(itemId, rulesetId, reportProvider)
        {
            this._visitors = new Dictionary<Guid, long>();
        }

        public new long GetExposure(Guid ruleId)
        {
            if (this._visitors.ContainsKey(ruleId))
            {
                return this._visitors[ruleId];
            }
            return 0L;
        }
    }
}
