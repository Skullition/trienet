// This code is distributed under MIT license. Copyright (c) 2013 George Mamaladze
// See license.txt or http://opensource.org/licenses/mit-license.php

namespace Gma.DataStructures.StringSearch
{
    public struct SplitResult
    {
        public SplitResult(StringPartition head, StringPartition rest)
        {
            Head = head;
            Rest = rest;
        }

        public StringPartition Rest { get; }
        public StringPartition Head { get; }

        public bool Equals(SplitResult other)
            => Head == other.Head && Rest == other.Rest;

        public override bool Equals(object obj)
        {
            if (obj is null) 
                return false;
            return obj is SplitResult result && Equals(result);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Head.GetHashCode()*397) ^ Rest.GetHashCode();
            }
        }

        public static bool operator ==(SplitResult left, SplitResult right)
            => left.Equals(right);

        public static bool operator !=(SplitResult left, SplitResult right)
            => !(left == right);
    }
}