using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Data.Models
{
    public enum ContributionType
    {
        Commit,
        InlineReviewComment,
        GeneralReviewComment,
        ApprovedReview,
        Review,
    }
}
