using System;
using UnityEngine;
using UnityEngine.Splines;

namespace MenuTree
{
    [Serializable]
    public struct MenuNodePair : IEquatable<MenuNodePair>
    {
        public MenuNode A;
        public MenuNode B;

        public MenuNodePair(MenuNode firstNode, MenuNode secondNode)
        {
            if (ReferenceEquals(firstNode, secondNode))
                throw new ArgumentException("Pair cannot contain the same node");

            if (firstNode.GetInstanceID() < secondNode.GetInstanceID())
            {
                A = firstNode;
                B = secondNode;
            }
            else
            {
                A = secondNode;
                B = firstNode;
            }
        }

        public bool Contains(MenuNode node)
        {
            return A == node || B == node;
        }

        public bool IsAllocated()
        {
            return A != null && B != null && A.IsAllocated && B.IsAllocated;
        }

        public bool Equals(MenuNodePair other)
        {
            return ReferenceEquals(A, other.A) && ReferenceEquals(B, other.B);
        }

        public override bool Equals(object obj)
        {
            return obj is MenuNodePair other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (A != null ? A.GetInstanceID() : 0);
                hash = hash * 31 + (B != null ? B.GetInstanceID() : 0);
                return hash;
            }
        }
    }

    [Serializable]
    public class MenuNodeConnectionData
    {
        public MenuNodePair pair;
        public SplineContainer spline;
    }
}
