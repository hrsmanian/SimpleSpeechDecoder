using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSpeechDecoder
{
    class Hmm
    {
        public string _label;
        public int _index;
        public int _nStates;
        public List<int> _stateIndices = new List<int>();
        public int _transIndex;
    }

    class HmmState
    {
        public const double STATE_LOG_SCORE = -23.02585093;
        public int _index;
        public int _nDimension;
        public int _nMixtures;
        public double[][] _mean;
        public double[][] _covar;
        public double[] _mixWeight;
        public double[] _scale;

        int _frameIndex = -1;
        double _score;

        public double Eval(double[] inFeat, int frame)
        {
            if (frame != _frameIndex)
            {
                // dummy variables
                //
                double tmp_score = 0.0;
                double tmp = 0.0;

                // assign frame index
                //
                _frameIndex = frame;

                // initialize score
                //
                _score = Double.MinValue;

                // loop over all mixture components
                //
                for (int i = 0; i < this._nMixtures; i++)
                {

                    // reset tmp score
                    //
                    tmp_score = _scale[i];

                    // compute the likelihood of data given mixture
                    //
                    for (int j = 0; j < inFeat.Length; j++)
                    {

                        // compute the translated data and take the product to evaluate
                        // likelihood score
                        //
                        double diff = inFeat[j] - _mean[i][j];
                        tmp_score += diff * diff * _covar[i][j];
                    }

                    // add the mixture weight, use recursive sum to protect against
                    // underflow
                    //
                    tmp_score = _mixWeight[i] - 0.5 * tmp_score;
                    if (_score < tmp_score)
                    {
                        tmp = _score;
                        _score = tmp_score;
                        tmp_score = tmp;
                    }
                    tmp = tmp_score - _score;
                    if (tmp >= STATE_LOG_SCORE)
                    {
                        _score += Math.Log(1.0 + Math.Exp(tmp));
                    }
                }
            }

            // return the log likelihood score
            //
            return _score;
        }
    }
}

