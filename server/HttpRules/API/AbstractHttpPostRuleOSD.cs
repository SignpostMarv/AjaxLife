using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AjaxLife.HttpRules.API
{
    public abstract class AbstractHttpPostRuleOSD : AbstractRuleOSD
    {
        public AbstractHttpPostRuleOSD(string path) : base(path)
        {
        }

        public override string[] Methods
        {
            get
            {
                return new string[]
                {
                    "POST"
                };
            }
        }
    }
}
