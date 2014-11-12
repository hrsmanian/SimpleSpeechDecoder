using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSpeechDecoder
{
    /// <summary>
    /// A linked list of word link records
    /// to keep track of the word history at word ends
    /// </summary>
    class WordLinkRecord
    {
        Int16 frame = -1;
        float score = float.MinValue;
        Int16 word_id = -1;
        WordLinkRecord parent = null;

        public Int16 Frame
        {
            get { return frame; }
            set { frame = value; }
        }

        public float Score
        {
            get { return score; }
            set { score = value; }
        }

        public Int16 WordId
        {
            get { return word_id; }
            set { word_id = value; }
        }

        public WordLinkRecord Parent
        {
            get { return parent; }
            set { parent = value; }
        }
    }

    class Instance
    {
        private Int16 _numStates;	 	/* num states N in this instance */
        public List<DecToken> _tok = new List<DecToken>();    /* token for each state (tok[0] to tok[N-1]) */
        private float _maxScore = float.NegativeInfinity;
        private float _exitScore = float.NegativeInfinity;

        public Int16 NumStates
        {
            get { return _numStates; }
            set { _numStates = value; }
        }

        public float MaxScore
        {
            get { return _maxScore; }
            set { _maxScore = value; }
        }

        public float ExitScore
        {
            get { return _exitScore; }
            set { _exitScore = value; }
        }
    }

    /// <summary>
    /// A simple token class
    /// This token is propagated in the model instance (Instance class)
    /// and across the network
    /// </summary>
    class DecToken
    {
        // lattice node for this token
        //
        private int _latNodeIndex = -1;

        // path score for the token
        //
        private float _pathScore = float.NegativeInfinity;
        private int _frameIndex;
        private WordLinkRecord _wlr = null;
        private float _entryScore = float.NegativeInfinity;

        public DecToken()
        {

        }

        public DecToken(float prob, int frame)
        {
            _pathScore = prob;
            _frameIndex = frame;
            _wlr = null;
            _entryScore = float.NegativeInfinity;
        }

        public int LatNodeIndex
        {
            get { return _latNodeIndex; }
            set { _latNodeIndex = value; }
        }

        public float PathScore
        {
            get { return _pathScore; }
            set { _pathScore = value; }
        }

        public int FrameIndex
        {
            get { return _frameIndex; }
            set { _frameIndex = value; }
        }

        public float EntryScore
        {
            get { return _entryScore; }
            set { _entryScore = value; }
        }

        public WordLinkRecord Wlr
        {
            get { return _wlr; }
            set { _wlr = value; }
        }
    }

    class Decoder
    {
        List<DecToken> _currTokens = null;
        List<DecToken> _nextTokens = null;
        Lattice _lattice = null;
        List<Hmm> _hmmList = null;
        Dictionary<int, double[][]> _transDict = null;

        WordLinkRecord _finalWlr = null;

        /// <summary>
        /// Init the decoder with the input lattice (network), models file and the transitions file
        /// </summary>
        /// <param name="lattice"></param>
        /// <param name="hmmList"></param>
        /// <param name="transDict"></param>
        public void Init(Lattice lattice, List<Hmm> hmmList, Dictionary<int, double[][]> transDict)
        {
            _lattice = lattice;
            _hmmList = hmmList;
            _transDict = transDict;

            _currTokens = new List<DecToken>();
            _nextTokens = new List<DecToken>();

            DecToken tmpToken = new DecToken();
            tmpToken.FrameIndex = 0;
            tmpToken.LatNodeIndex = _lattice._enter._index;

            tmpToken.EntryScore = 0.0f;
            tmpToken.Wlr = null;
            _nextTokens.Add(tmpToken);
        }

        /// <summary>
        /// Decode a single model instance, find the best exit score and the best state
        /// with that score
        /// </summary>
        /// <param name="latticeNode"></param>
        /// <param name="statesList"></param>
        /// <param name="inFeat"></param>
        /// <param name="frameIndex"></param>
        void StepModels(LatticeNode latticeNode, List<HmmState> statesList, double[] inFeat, int frameIndex)
        {
            int numModels = _hmmList.Count;
            double maxExitScore = Double.NegativeInfinity;
            int modelIndexMax = 1;

            int modelIndex = latticeNode._hmmIndex;
            Instance insts = latticeNode._insts;
            Instance tmpInsts = latticeNode._tmpInsts;

            Hmm hmm = _hmmList[modelIndex];
            double[][] trans = _transDict[hmm._transIndex];
            int N = latticeNode._insts.NumStates;

            double maxScore = Double.NegativeInfinity, score;
            int maxStIndex = -1;

            // loop over states from first emitting state to last
            // index 1 through N-1
            //
            for (int j = 1; j < N - 1; j++)
            {
                maxScore = Double.NegativeInfinity;
                maxStIndex = -1;

                for (int i = 0; i <= j; i++)
                {
                    score = latticeNode._insts._tok[i].PathScore + Math.Log10(trans[i][j]);

                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxStIndex = i;
                    }
                }

                if (maxStIndex != -1)
                {
                    // copy that token into state jd
                    latticeNode._tmpInsts._tok[j].PathScore = latticeNode._insts._tok[maxStIndex].PathScore;
                    latticeNode._tmpInsts._tok[j].FrameIndex = latticeNode._insts._tok[maxStIndex].FrameIndex;
                    latticeNode._tmpInsts._tok[j].Wlr = latticeNode._insts._tok[maxStIndex].Wlr;

                    HmmState hmmState = statesList[hmm._stateIndices[j]];
                    double stateScore = hmmState.Eval(inFeat, frameIndex);
                    latticeNode._tmpInsts._tok[j].PathScore += ((float)(Math.Log10(trans[maxStIndex][j])) + (float)stateScore);
                }
            }

            // find the maximum score for this instance
            //
            for (int j = 1; j < N - 1; j++)
            {
                insts._tok[j].PathScore = tmpInsts._tok[j].PathScore;
                insts._tok[j].FrameIndex = tmpInsts._tok[j].FrameIndex;
                insts._tok[j].Wlr = tmpInsts._tok[j].Wlr;

                if (insts._tok[j].PathScore > insts.MaxScore)
                {
                    insts.MaxScore = (float)insts._tok[j].PathScore;
                }
            }

            maxStIndex = -1;
            maxScore = Double.NegativeInfinity;

            // find the state within this hmm that has the max score
            //
            for (int j = 1; j < N - 1; j++)
            {

                // find pred state jmax in model imax for which prob + log a is maximum
                score = insts._tok[j].PathScore + Math.Log10(trans[j][N - 1]);

                if (score > maxScore)
                {
                    maxScore = score;
                    maxStIndex = j;
                }

                if (maxScore > maxExitScore)
                {
                    maxExitScore = maxScore;
                    modelIndexMax = modelIndex;
                }
            }

            if (maxStIndex != -1)
            {
                insts._tok[N - 1].PathScore = insts._tok[maxStIndex].PathScore;
                insts._tok[N - 1].PathScore += (float)Math.Log10(trans[maxStIndex][N - 1]);

                insts._tok[N - 1].Wlr = insts._tok[maxStIndex].Wlr;
            }

            insts.ExitScore = insts._tok[N - 1].PathScore;

            return; ;
        }

        /// <summary>
        /// Given the feature vector, evaluate all the active tokens
        /// </summary>
        /// <param name="inFeat"></param>
        /// <param name="frameIndex"></param>
        /// <param name="statesList"></param>
        public void EvalTokens(double[] inFeat, int frameIndex, List<HmmState> statesList)
        {
            _currTokens.Clear();
            _currTokens.AddRange(_nextTokens);
            _nextTokens.Clear();

            foreach (DecToken token in _currTokens)
            {
                LatticeNode latNode = _lattice._nodeList[token.LatNodeIndex];

                if (latNode._nodeType == LatticeNodeType.PhoneNode)
                {
                    latNode._insts._tok[0].Wlr = token.Wlr;
                    latNode._insts._tok[0].PathScore = token.EntryScore;
                    StepModels(latNode, statesList, inFeat, frameIndex);
                    token.PathScore = latNode._insts.ExitScore;
                    token.Wlr = latNode._insts._tok[latNode._insts.NumStates - 1].Wlr;
                }
            }

        }

        /// <summary>
        /// Propagate the active tokens to the next emitting state
        /// </summary>
        /// <param name="frameIndex"></param>
        public void PropagateTokens(int frameIndex)
        {
            for (int i = 0; i < _currTokens.Count; i++)
            {
                DecToken token = _currTokens[i];

                PropagateTokensInternal(token, frameIndex);
            }
        }

        /// <summary>
        /// Propagate a token to the next emitting state recursively
        /// Apply viterbi pruning and keep only one best token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="frameIndex"></param>
        public void PropagateTokensInternal(DecToken token, int frameIndex)
        {
            LatticeNode latNode = _lattice._nodeList[token.LatNodeIndex];

            if (latNode._index == _lattice._enter._index)
            {
                DecToken tmpToken = new DecToken();
                tmpToken.LatNodeIndex = latNode._index;
                tmpToken.FrameIndex = (short)frameIndex;
                tmpToken.Wlr = token.Wlr;
                token.EntryScore = float.NegativeInfinity;
                _nextTokens.Add(tmpToken);
            }

            // if we are at the end of the model, propagate it to the connected model
            //
            if (token.PathScore > float.NegativeInfinity)
            {
                foreach (int outArcIndex in latNode._outArcs)
                {
                    LatticeArc outArc = _lattice._arcList[outArcIndex];
                    LatticeNode fromNode = _lattice._nodeList[outArc._fromNodeIndex];
                    LatticeNode toNode = _lattice._nodeList[outArc._toNodeIndex];

                    //// add word insertion penalty if we are going from a phone node to word node
                    ////
                    //if (fromNode._nodeType == LatticeNodeType.PhoneNode &&
                    //    toNode._nodeType == LatticeNodeType.WordNode)
                    //{
                    //    token._pathScore += -10000;
                    //}

                    if (toNode._nodeType != LatticeNodeType.PhoneNode)
                    {
                        // propagate the token till we reach a phone node i.e emitting state
                        //
                        DecToken tmpToken = new DecToken();
                        tmpToken.LatNodeIndex = toNode._index;
                        tmpToken.EntryScore = tmpToken.PathScore = token.PathScore;
                        tmpToken.FrameIndex = (short)frameIndex;
                        tmpToken.Wlr = token.Wlr;


                        if (fromNode._nodeType == LatticeNodeType.WordNode &&
                            fromNode._label != "!NULL")
                        {
                            WordLinkRecord wlr = new WordLinkRecord();
                            wlr.Parent = token.Wlr;
                            wlr.WordId = (short)_lattice._nodeList[token.LatNodeIndex]._wordId;
                            wlr.Score = token.PathScore;
                            wlr.Frame = (Int16)tmpToken.FrameIndex;
                            tmpToken.Wlr = wlr;
                        }

                        PropagateTokensInternal(tmpToken, frameIndex);
                    }
                    else
                    {
                        DecToken tmpToken = _nextTokens.FirstOrDefault((tok) => (_lattice._nodeList[tok.LatNodeIndex]._index == toNode._index));
                        if (tmpToken != null)
                        {
                            if (tmpToken.EntryScore < (token.PathScore))
                            {
                                tmpToken.EntryScore = token.PathScore;
                                tmpToken.Wlr = token.Wlr;

                                if (fromNode._nodeType == LatticeNodeType.WordNode &&
                                    fromNode._label != "!NULL")
                                {
                                    WordLinkRecord wlr = new WordLinkRecord();
                                    wlr.Parent = token.Wlr;
                                    wlr.WordId = (short)_lattice._nodeList[token.LatNodeIndex]._wordId;
                                    wlr.Score= token.PathScore;
                                    wlr.Frame = (Int16)tmpToken.FrameIndex;
                                    tmpToken.Wlr = wlr;
                                }
                            }
                        }
                        else
                        {
                            tmpToken = new DecToken();
                            tmpToken.LatNodeIndex = toNode._index;
                            tmpToken.EntryScore = token.PathScore;
                            tmpToken.FrameIndex = (short)frameIndex;
                            tmpToken.Wlr = token.Wlr;

                            if (fromNode._nodeType == LatticeNodeType.WordNode &&
                                fromNode._label != "!NULL")
                            {
                                WordLinkRecord wlr = new WordLinkRecord();
                                wlr.Parent = token.Wlr;
                                wlr.WordId = (short)_lattice._nodeList[token.LatNodeIndex]._wordId;
                                wlr.Score = token.PathScore;
                                wlr.Frame = (Int16)tmpToken.FrameIndex;
                                tmpToken.Wlr = wlr;
                            }

                            _nextTokens.Add(tmpToken);
                        }


                    }
                }
            }

            if (latNode._outArcs.Count == 0)
            {
                _finalWlr = token.Wlr;
            }
        }

        public void BackTrace(Dict dict)
        {
            List<Tuple<string, int, float>> tupleList = new List<Tuple<string, int, float>>();

            WordLinkRecord wlr = _finalWlr;
            while (wlr != null)
            {
                // ignore NULL nodes
                //
                if (wlr.WordId!= -1)
                {
                    Tuple<string, int, float> tuple = new Tuple<string, int, float>(dict._id2WordDict[wlr.WordId], wlr.Frame, wlr.Score);
                    tupleList.Add(tuple);
                }
                wlr = wlr.Parent;
            }

            tupleList.Reverse();

            float prevScore = 0.0f;
            foreach (Tuple<string, int, float> tuple in tupleList)
            {
                Console.WriteLine(tuple.Item1 + "\t" + tuple.Item2 + "\t" + (tuple.Item3 - prevScore));
                prevScore = tuple.Item3;
            }

        }
    }


}

