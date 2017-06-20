namespace AjaxLife.HttpRules
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
