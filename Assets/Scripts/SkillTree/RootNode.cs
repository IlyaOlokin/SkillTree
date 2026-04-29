namespace SkillTree
{
    public class RootNode : Node
    {
        public override bool IsAllocated => true;
        public override bool IsActive => true;
        
        protected override bool HasRootConnection() => true;
    }
}
