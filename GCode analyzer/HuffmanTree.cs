using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Martin.GCode
{
    public class HuffmanTree
    {
        private List<Node> nodes = new List<Node>();
        public Node Root { get; set; }

        public int maxdepth()
        {
            return maxdepth(Root);
        }

        int maxdepth(Node n)
        {
            if (n.IsLeaf()) return 1;
            int left = 0, right = 0;
            if (n.Left != null)
                left = 1 + maxdepth(n.Left);
            if (n.Right != null)
                right = 1 + maxdepth(n.Left);
            return (right > left ? right : left);
        }

        public int Bits(string symbol)
        {
            return Root.Traverse(symbol, new List<bool>()).Count;
        }

        public static HuffmanTree Build(Dictionary<string, int> Frequencies)
        {
            HuffmanTree tree = new HuffmanTree();
            foreach (KeyValuePair<string, int> symbol in Frequencies)
            {
                tree.nodes.Add(new Node() { Symbol = symbol.Key, Frequency = symbol.Value });
            }

            while (tree.nodes.Count > 1)
            {
                List<Node> orderedNodes = tree.nodes.OrderBy(node => node.Frequency).ToList<Node>();

                if (orderedNodes.Count >= 2)
                {
                    // Take first two items
                    List<Node> taken = orderedNodes.Take(2).ToList<Node>();

                    // Create a parent node by combining the frequencies
                    Node parent = new Node()
                    {
                        Symbol = "*",
                        Frequency = taken[0].Frequency + taken[1].Frequency,
                        Left = taken[0],
                        Right = taken[1]
                    };

                    tree.nodes.Remove(taken[0]);
                    tree.nodes.Remove(taken[1]);
                    tree.nodes.Add(parent);
                }

                tree.Root = tree.nodes.FirstOrDefault();

            }

            return tree;
        }

        public BitArray Encode(string source)
        {
            return new BitArray(this.Root.Traverse(source, new List<bool>()).ToArray());
        }

        public string Decode(BitArray bits)
        {
            Node current = this.Root;
            string decoded = "";

            foreach (bool bit in bits)
            {
                if (bit)
                {
                    if (current.Right != null)
                    {
                        current = current.Right;
                    }
                }
                else
                {
                    if (current.Left != null)
                    {
                        current = current.Left;
                    }
                }

                if (current.IsLeaf())
                {
                    decoded += current.Symbol;
                    current = this.Root;
                }
            }

            return decoded;
        }
    }
}
