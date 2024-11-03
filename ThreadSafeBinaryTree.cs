using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadSafeBinaryTreeApp
{
    public class ThreadSafeBinaryTree : IBinaryTree
    {
        public class Node
        {
            public string Value;
            public int Count;
            public Node? Left;
            public Node? Right;

            public Node(string value)
            {
                Value = value;
                Count = 1;
                Left = null;
                Right = null;
            }
        }

        private Node? Root;
        private readonly Mutex counterMutex;
        private readonly Semaphore writeMutex;
        private int counter;

        public ThreadSafeBinaryTree()
        {
            Root = null;
            counterMutex = new Mutex();
            writeMutex = new Semaphore(1, 1);
            counter = 0;
        }

        public void Add(string value)
        {
            writeMutex.WaitOne();
            if (Root == null)
            {
                Root = new Node(value);
                writeMutex.Release();
                return;
            }
            AddHelper(value, Root);
            writeMutex.Release();
        }

        private void AddHelper(string value, Node tree)
        {
            if (tree == null)
                return;

            int compare = string.Compare(value, tree.Value, StringComparison.Ordinal);
            if (compare == 0)
            {
                tree.Count++;
                return;
            }
            if (compare < 0)
            {
                if (tree.Left == null)
                {
                    tree.Left = new Node(value);
                    return;
                }
                else
                {
                    AddHelper(value, tree.Left);
                }
            }
            else
            {
                if (tree.Right == null)
                {
                    tree.Right = new Node(value);
                    return;
                }
                else
                {
                    AddHelper(value, tree.Right);
                }
            }
        }

        public void Delete(string value)
        {
            writeMutex.WaitOne();
            if (Root == null)
            {
                writeMutex.Release();
                return;
            }
            Root = DeleteHelper(value, Root);
            writeMutex.Release();
        }

        private Node? DeleteHelper(string value, Node? tree)
        {
            if (tree == null)
                return null;

            int compare = string.Compare(value, tree.Value, StringComparison.Ordinal);
            if (compare < 0)
            {
                tree.Left = DeleteHelper(value, tree.Left);
            }
            else if (compare > 0)
            {
                tree.Right = DeleteHelper(value, tree.Right);
            }
            else
            {
                if (tree.Count > 1)
                {
                    tree.Count--;
                    return tree;
                }
                if (tree.Left == null)
                    return tree.Right;
                if (tree.Right == null)
                    return tree.Left;

                Node min = GetMin(tree.Right);
                tree.Value = min.Value;
                tree.Count = min.Count;
                tree.Right = DeleteHelper(min.Value, tree.Right);
            }
            return tree;
        }

        private Node GetMin(Node node)
        {
            while (node.Left != null)
                node = node.Left;
            return node;
        }

        public int Search(string value)
        {
            int result = 0;
            counterMutex.WaitOne();
            counter++;
            if (counter == 1)
                writeMutex.WaitOne();
            counterMutex.ReleaseMutex();

            if (Root != null)
            {
                result = SearchHelper(value, Root);
            }

            counterMutex.WaitOne();
            counter--;
            if (counter == 0)
                writeMutex.Release();
            counterMutex.ReleaseMutex();

            return result;
        }

        private int SearchHelper(string value, Node tree)
        {
            if (tree == null)
                return 0;

            int compare = string.Compare(value, tree.Value, StringComparison.Ordinal);
            if (compare == 0)
                return tree.Count;
            if (compare < 0)
                return SearchHelper(value, tree.Left);
            else
                return SearchHelper(value, tree.Right);
        }

        public void PrintSorted()
        {
            if (Root == null)
                return;

            counterMutex.WaitOne();
            counter++;
            if (counter == 1)
                writeMutex.WaitOne();
            counterMutex.ReleaseMutex();

            PrintSortedHelper(Root);

            counterMutex.WaitOne();
            counter--;
            if (counter == 0)
                writeMutex.Release();
            counterMutex.ReleaseMutex();
        }

        private void PrintSortedHelper(Node node)
        {
            if (node == null)
                return;

            PrintSortedHelper(node.Left);
            Console.WriteLine($"{node.Value} ({node.Count})");
            PrintSortedHelper(node.Right);
        }
    }
}
