# InfiniteNode

[Home](../Home.md) · [Skill tree and progression](../Systems/SkillTree.md)

Use `InfiniteNode` instead of `Node` on the future prefab; keep the usual node input,
visual and tooltip components. Assign a TextMeshPro text object to `Skill Points Text`.
The optional text shows `InvestedSkillPoints`, including zero.

Configure ordinary `BaseModifier` assets containing scalar `Added` or `Increased`
values. `More`, damage-type masks and special modifiers are unsupported: the editor
reports them, allocation is rejected and they cannot contribute effects from saves.

Each left click invests one skill point; each right click refunds one. At zero the
node is unallocated. Removing the final point follows existing dependent-branch
removal rules. Removing a parent branch refunds the entire investment of any
dependent infinite nodes. Free independent allocation is disabled for this type.

Effects and green tooltip numbers scale by invested points, independently of Power.
An unallocated node previews one point's effect without highlighting. For example,
5% per point gives 50% at 10 points and 500% at 100 points.

Save data stores counts by node ID and migrates their IDs alongside other node data.
Older saves without counts restore allocated infinite nodes with one point.

Run `Tools > Skill Tree > Validate Infinite Node` outside Play Mode to validate in
an isolated preview scene. The result is written to `Logs/InfiniteNodeValidation.txt`.
