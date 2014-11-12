using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleSpeechDecoder
{
    enum LatticeNodeType
    {
        NormalNode = 0,
        WordNode = 1,
        HmmNode,
        HmmStartNode,
        HmmEndNode,
        PhoneNode,
        UnusedNode
    }

    class LatticeNode
    {
        public Int16 _index = -1;
        public string _label = String.Empty;
        public LatticeNodeType _nodeType = LatticeNodeType.NormalNode;
        public Int16 _wordId = -1;
        public Int16 _hmmIndex = -1;

        public List<int> _inArcs = new List<int>();
        public List<int> _outArcs = new List<int>();

        public Instance _insts = null;
        public Instance _tmpInsts = null;

    }

    class LatticeArc
    {
        public int _index;
        public int _fromNodeIndex;
        public int _toNodeIndex;
        public double _score = 0.0;
    }

    class Lattice
    {
        public LatticeNode _enter;
        public LatticeNode _exit;

        public List<LatticeNode> _nodeList;
        public List<LatticeArc> _arcList;

        public Lattice()
        {
            
        }

        private LatticeNode CreateLatticeNode(int index, string label)
        {
            LatticeNode latNode = new LatticeNode();
            latNode._nodeType = LatticeNodeType.NormalNode;
            latNode._index = (short)index;
            latNode._label = label;

            return latNode;

        }

        public bool Read(string fileName)
        {
            if (fileName == null ||
               (!File.Exists(fileName)))
            {
                return false;
            }

            TextReader reader = new StreamReader(fileName);
            char[] splitChars = new char[] {' ' };
            string line = null;
            int nNodes = 0;
            int nArcs = 0;
            do
            {
                line = reader.ReadLine();
                if (line != null)
                {
                    if (line.StartsWith("N="))
                    {
                        string[] cols = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                        nNodes = Convert.ToInt32(cols[0].Split('=').LastOrDefault());
                        nArcs = Convert.ToInt32(cols[1].Split('=').LastOrDefault());

                        _nodeList = new List<LatticeNode>(nNodes);
                        _arcList = new List<LatticeArc>(nArcs);
                    }

                    else if (line.StartsWith("I="))
                    {
                        string[] cols = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                        
                        int nodeIndex = Convert.ToInt32(cols[0].Split('=').LastOrDefault());
                        string label = cols[1].Split('=').LastOrDefault();

                        LatticeNode node = CreateLatticeNode(nodeIndex, label);
                        node._nodeType = LatticeNodeType.WordNode;
                        _nodeList.Add(node);
                    }

                    else if (line.StartsWith("J="))
                    {
                        string[] cols = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                        int arcIndex = Convert.ToInt32(cols[0].Split('=').LastOrDefault());
                        int startNodeIndex = Convert.ToInt32(cols[1].Split('=').LastOrDefault());
                        int endNodeIndex = Convert.ToInt32(cols[2].Split('=').LastOrDefault());

                        LatticeNode fromNode = _nodeList.Find((thisNode) => (thisNode._index == startNodeIndex));
                        LatticeNode toNode = _nodeList.Find((thisNode) => (thisNode._index == endNodeIndex));
                        LatticeArc arc = new LatticeArc();
                        arc._index = arcIndex;
                        arc._fromNodeIndex = startNodeIndex;
                        arc._toNodeIndex = endNodeIndex;
                        _arcList.Add(arc);

                        fromNode._outArcs.Add(_arcList.Count-1);
                        toNode._inArcs.Add(_arcList.Count - 1);
                    }
                }
            } while (line != null);

            for (int i = 0; i < _nodeList.Count; i++)
            {
                if (_nodeList[i]._inArcs.Count == 0)
                {
                    this._enter = _nodeList[i];
                }

                if (_nodeList[i]._outArcs.Count == 0)
                {
                    this._exit = _nodeList[i];
                }
            }

            reader.Close();

            return true;
        }


        public void ExpandWords(Dict dict, List<Hmm> hmmList)
        {
            int nodeCount = _nodeList.Count;
            for (int i = 0; i < nodeCount; i++)
            {
                LatticeNode latNode = _nodeList[i];

                if (!(latNode._label == "!NULL"))
                {
                    if (dict._wordDict.ContainsKey(latNode._label))
                    {
                        int wordId = dict._wordDict[latNode._label];
                        latNode._wordId = (short)wordId;
                        List<string> prons = dict._pronDict[wordId];

                        LatticeNode prevNode = null;
                        int j = 0;
                        foreach (string pron in prons)
                        {
                            LatticeNode currNode = CreateLatticeNode(_nodeList.Count, pron);
                            // currNode._wordId = wordId;
                            currNode._nodeType = LatticeNodeType.PhoneNode;
                            currNode._index = (short)_nodeList.Count;
                            _nodeList.Add(currNode);
                            if (j == 0)
                            {
                                foreach (int inArcIndex in latNode._inArcs)
                                {
                                    _arcList[inArcIndex]._toNodeIndex = currNode._index;
                                    currNode._inArcs.Add(inArcIndex);
                                }
                            }
                            else
                            {
                                LatticeArc arc = new LatticeArc();
                                arc._index = _arcList.Count;
                                arc._fromNodeIndex = prevNode._index;
                                arc._toNodeIndex = currNode._index;
                                arc._score = 0.0;
                                prevNode._outArcs.Add(arc._index);
                                currNode._inArcs.Add(arc._index);
                                _arcList.Add(arc);
                            }

                            // add model instance
                            //
                            Hmm hmm = hmmList.FirstOrDefault((tmpHmm) => (tmpHmm._label == pron));
                            if (hmm != null)
                            {
                                currNode._hmmIndex = (short)hmm._index;
                                currNode._insts = new Instance();
                                currNode._tmpInsts = new Instance();

                                currNode._insts.NumStates = (short)hmm._nStates;
                                currNode._insts._tok.Add(new DecToken(0.0f, 0));
                                currNode._insts.MaxScore = float.NegativeInfinity;

                                currNode._tmpInsts.NumStates = (short)hmm._nStates;
                                currNode._tmpInsts._tok.Add(new DecToken(0.0f, 0));
                                currNode._tmpInsts.MaxScore = float.NegativeInfinity;

                                for (j = 1; j < currNode._insts.NumStates; j++)
                                {
                                    currNode._insts._tok.Add(new DecToken(float.NegativeInfinity, 0));
                                    currNode._tmpInsts._tok.Add(new DecToken(float.NegativeInfinity, 0));
                                }
                            }

                            prevNode = currNode;
                            j++;
                        }

                        latNode._inArcs.RemoveRange(0, latNode._inArcs.Count);

                        LatticeArc finalArc = new LatticeArc();
                        finalArc._index = _arcList.Count;
                        finalArc._fromNodeIndex = prevNode._index;
                        finalArc._toNodeIndex = latNode._index;
                        prevNode._outArcs.Add(finalArc._index);
                        latNode._inArcs.Add(finalArc._index);
                        _arcList.Add(finalArc);

                    }
                }
            }

            for (int i = 0; i < _nodeList.Count; i++)
            {
                if (_nodeList[i]._inArcs.Count == 0)
                {
                    this._enter = _nodeList[i];
                }

                if (_nodeList[i]._outArcs.Count == 0)
                {
                    this._exit = _nodeList[i];
                }
            }

        }


        public void RemoveUnusedNodes()
        {
            List<LatticeNode> removeList = _nodeList.FindAll((node) => (node._nodeType == LatticeNodeType.UnusedNode));
            foreach (LatticeNode node in removeList)
            {
                _nodeList.Remove(node);
            }
        }

    }
}
