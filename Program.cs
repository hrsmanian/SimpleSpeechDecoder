using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleSpeechDecoder
{
    class Dict
    {
        public Dictionary<string, int> _wordDict = new Dictionary<string, int>();
        public Dictionary<int, List<string>> _pronDict = new Dictionary<int, List<string>>();
        public Dictionary<int, string> _id2WordDict = new Dictionary<int, string>();
        public Dict()
        {

        }

        public bool ReadDict(string fileName)
        {
            if (fileName == null || fileName == String.Empty ||
                !File.Exists(fileName))
            {
                Console.WriteLine("Lexicon file not valid. Kindly check input");
                return false;
            }

            TextReader reader = new StreamReader(fileName);
            char[] splitChars = new char[] { ' ' };
            string line = null;
            int wordIndex = 0;
            do
            {
                line = reader.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    string[] cols = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                    _wordDict.Add(cols[0], wordIndex);
                    _id2WordDict.Add(wordIndex, cols[0]);
                    List<string> prons = new List<string>();
                    for (int i = 1; i < cols.Length; i++)
                    {
                        prons.Add(cols[i]);
                    }

                    _pronDict.Add(wordIndex, prons);
                    wordIndex++;
                }
            } while (line != null);

            reader.Close();

            return true;
        }
    }

    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("SimpleSpeechDecoder.exe <lexicon-text-file> <models-text-file> <states-text-file> <transitions-text-file> <network-text-file> <input-mfcc-bin-file> ");
        }

        static void Main(string[] args)
        {
            if ((args.Length == 1 && args[0] == "help") ||
                (args.Length != 6))
            {
                PrintUsage();
                return;
            }

            // read the lexicon
            //
            Dict dict = new Dict();
            if (!dict.ReadDict(args[0]))
            {
                Console.WriteLine("Loading Lexicon failed");
                return;
            }

            // load the models, states and transitions files
            //
            List<Hmm> hmmList = Utils.ReadModelsFile(args[1]);
            if (hmmList == null)
            {
                Console.WriteLine("Loading Models file failed");
                return;
            }
            
            List<HmmState> statesList = Utils.ReadStatesFile(args[2]);
            if (statesList == null)
            {
                Console.WriteLine("Loading States file failed");
                return;
            }

            Dictionary<int, double[][]> transDict = Utils.ReadTransitionsFile(args[3]);
            if (transDict == null)
            {
                Console.WriteLine("Loading Transitions file failed");
                return;
            }

            // load the static graph
            //
            Lattice lat = new Lattice();
            if (lat.Read(args[4]))
            {
                lat.ExpandWords(dict, hmmList);
            }
            else
            {
                Console.WriteLine("Loading Network file failed");
                return;
            }


            Decoder decoder = new Decoder();
            decoder.Init(lat, hmmList, transDict);

            int dim = statesList[0]._nDimension;
            int frameCount = 0;
            int lenRead = 0;

            if (!File.Exists(args[5]))
            {
                Console.WriteLine("Input Mfcc file not found");
                return;
            }

            FileStream fStream = File.Open(args[5], FileMode.Open);
            using (BinaryReader binaryReader = new BinaryReader(fStream))
            {
                long len = binaryReader.BaseStream.Length;
                double[] inFeatures = new double[statesList[0]._nDimension];
                while (lenRead < len)
                {
                    int featRead = 0;
                    while ((lenRead < len) && (featRead < dim))
                    {
                        inFeatures[featRead] = binaryReader.ReadDouble();
                        featRead++;
                        lenRead += sizeof(double);
                    }

                    decoder.EvalTokens(inFeatures, frameCount, statesList);
                    decoder.PropagateTokens(frameCount);
                    frameCount++;

                }

                decoder.BackTrace(dict);

                // Console.ReadLine();
            }
            fStream.Close();
        }
    }
}
