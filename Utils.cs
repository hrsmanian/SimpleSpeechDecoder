using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleSpeechDecoder
{
    class Utils
    {
        public static int MAX_MODELS = 13;
        public static int MAX_STATES = 17;

        public static List<Hmm> ReadModelsFile(string fileName)
        {
            if (fileName == null ||
                (!File.Exists(fileName)))
            {
                return null;
            }

            TextReader reader = new StreamReader(fileName);
            char[] splitChars = new char[] { ' ' };
            string line = null;
            List<Hmm> hmmList = new List<Hmm>();
            do
            {
                line = reader.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    if (line.Trim().StartsWith("num_models"))
                    {
                        string[] cols = line.Split('=');
                    }
                    else if (line.Trim().StartsWith("index"))
                    {
                        Hmm hmm = new Hmm();
                        string[] cols = line.Split(':');
                        hmm._index = Convert.ToInt32(cols[1].Trim());

                        line = reader.ReadLine();
                        string[] cols1 = line.Split(':');
                        hmm._label = cols1[1].Trim();

                        line = reader.ReadLine();
                        string[] cols2 = line.Split(':');
                        hmm._nStates = Convert.ToInt32(cols2[1].Trim());

                        line = reader.ReadLine();
                        string[] cols3 = line.Split(':');
                        hmm._transIndex = Convert.ToInt32(cols3[1].Trim());

                        line = reader.ReadLine();
                        string[] cols4 = line.Split(':').Last().Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string index in cols4)
                        {
                            hmm._stateIndices.Add(Convert.ToInt32(index));
                        }

                        hmmList.Add(hmm);


                    }
                }
            } while (line != null);

            return hmmList;
        }

        public static List<HmmState> ReadStatesFile(string fileName)
        {
            if (fileName == null ||
                (!File.Exists(fileName)))
            {
                return null;
            }

            TextReader reader = new StreamReader(fileName);
            char[] splitChars = new char[] { ' ' };
            string line = null;
            List<HmmState> statesList = new List<HmmState>();
            int nDims = 0;
            int nStates = 0;
            do
            {
                line = reader.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    if (line.Trim().StartsWith("feature_size"))
                    {
                        string[] cols = line.Split('=');
                        nDims = Convert.ToInt32(cols[1]);

                        line = reader.ReadLine();
                        string[] cols1 = line.Split('=');

                        nStates = Convert.ToInt32(cols1[1].Trim());

                    }
                    else if (line.Trim().StartsWith("State"))
                    {
                        HmmState state = new HmmState();
                        statesList.Add(state);
                        state._nDimension = nDims;

                        string[] cols2 = line.Split(':');
                        state._index = Convert.ToInt32(cols2[1].Trim());
                    }
                    else if (line.Trim().StartsWith("nummixes"))
                    {
                        string[] cols2 = line.Split(':');
                        statesList[statesList.Count - 1]._nMixtures = Convert.ToInt32(cols2[1].Trim());
                        if (statesList[statesList.Count - 1]._nMixtures > 0)
                        {
                            statesList[statesList.Count - 1]._mean = new double[statesList[statesList.Count - 1]._nMixtures][];
                            statesList[statesList.Count - 1]._covar = new double[statesList[statesList.Count - 1]._nMixtures][];
                            statesList[statesList.Count - 1]._mixWeight = new double[statesList[statesList.Count - 1]._nMixtures];
                            statesList[statesList.Count - 1]._scale = new double[statesList[statesList.Count - 1]._nMixtures];
                        }
                    }
                    else if (line.Trim().StartsWith("mixture"))
                    {
                        string[] cols2 = line.Split(':');
                        int mixIndex = Convert.ToInt32(cols2[1]);

                        line = reader.ReadLine();
                        cols2 = line.Split(':');
                        statesList[statesList.Count - 1]._mixWeight[mixIndex - 1] = Convert.ToDouble(cols2[1].Trim());

                        line = reader.ReadLine();
                        cols2 = line.Split(':');

                        string[] cols3 = cols2[1].Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                        statesList[statesList.Count - 1]._mean[mixIndex - 1] = new double[nDims];
                        for (int k = 0; k < cols3.Length; k++)
                        {
                            statesList[statesList.Count - 1]._mean[mixIndex - 1][k] = Convert.ToDouble(cols3[k]);
                        }

                        line = reader.ReadLine();
                        cols2 = line.Split(':');

                        cols3 = cols2[1].Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                        statesList[statesList.Count - 1]._covar[mixIndex - 1] = new double[nDims];
                        for (int k = 0; k < cols3.Length; k++)
                        {
                            statesList[statesList.Count - 1]._covar[mixIndex - 1][k] = Convert.ToDouble(cols3[k]);
                        }

                        line = reader.ReadLine();
                        cols2 = line.Split(':');

                        statesList[statesList.Count - 1]._scale[mixIndex - 1] = Convert.ToDouble(cols2[1]);

                    }
                }
            } while (line != null);

            return statesList;
        }

        public static Dictionary<int, double[][]> ReadTransitionsFile(string fileName)
        {
            if (fileName == null ||
                (!File.Exists(fileName)))
            {
                return null;
            }

            TextReader reader = new StreamReader(fileName);
            char[] splitChars = new char[] { ' ' };
            string line = null;
            Dictionary<int, double[][]> transDict = new Dictionary<int, double[][]>();
            do
            {
                line = reader.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    if (line.Trim().StartsWith("num_transitions"))
                    {
                        string[] cols = line.Split('=');
                    }
                    else
                    {
                        string[] cols = line.Split('.');
                        int nRows = Convert.ToInt32(cols[1].Trim());
                        int transIndex = Convert.ToInt32(cols[0].Trim());
                        double[][] trans = new double[nRows][];

                        for (int i = 0; i < nRows; i++)
                        {

                            line = reader.ReadLine();
                            string[] cols1 = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                            trans[i] = new double[cols1.Length];
                            for (int k = 0; k < cols1.Length; k++)
                            {
                                trans[i][k] = Convert.ToDouble(cols1[k].Trim());
                            }
                        }

                        transDict.Add(transIndex, trans);

                    }
                }
            } while (line != null);

            reader.Close();

            return transDict;
        }
    }
}
