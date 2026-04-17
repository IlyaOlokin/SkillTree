namespace MenuTree
{
    public class MenuBackNodeAction : MenuNodeAction
    {
        protected override void OnAllocated(MenuNode node)
        {
            node.TreeController?.ResetToRoot();
        }
    }
}
