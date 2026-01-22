namespace SkillTree
{
    public class RootNode : Node
    {
        public override bool IsAllocated => true;
        
        protected override bool HasRootConnection() => true;
    }
}
