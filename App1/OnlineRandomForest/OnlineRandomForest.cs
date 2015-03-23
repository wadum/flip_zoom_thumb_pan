using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OnlineRandomForest
{
    public class OnlineRandomForest : IRandomForest
    {

        int _t = 100; // Size of Forest
        int _alpha = 10; // Minimum number of samples for Split
        float _beta = 0.1f; // Minimum Gain
        bool _updateAlpha = false;
        List<ORFTree> _trees = new List<ORFTree>();
        int _numberOfTests = 10;
        public bool Done { get { return _trees.All(d => d.Done); } }
        public float PercentDone { get { return ((float)_trees.Count(d => d.Done) / (float)_trees.Count) * 100; } }
        int _numberOfFeatures = 0;
        RandomForestType type = RandomForestType.OnlineRandomForest;
        public RandomForestType ForestType { get { return type; } }


        public OnlineRandomForest()
        {
            _t = 100;
            _alpha = 2;
            _beta = 0.05f;
            _numberOfTests = 10;
            Random rand = new Random();
            for (int i = 0; i < _t; i++)
            {
                _trees.Add(new ORFTree(_numberOfTests, i, rand.Next(10000)));
            }
        }

        /// <summary>
        /// Creates an Online Random Forest
        /// </summary>
        /// <param name="t">Number of Trees in the forest</param>
        /// <param name="alpha">Number of samples to have seen before being able to split a node</param>
        /// <param name="beta">Minimum Gain to have to split a node</param>
        /// <param name="numberOfTests">Number of different tests in each node</param>
        public OnlineRandomForest(int t = 100, int alpha = 2, float beta = 0.05f, int numberOfTests = 10)
        {
            _t = t;
            _alpha = alpha;
            _beta = beta;
            _numberOfTests = numberOfTests;
            Random rand = new Random();
            for (int i = 0; i < t; i++)
            {
                _trees.Add(new ORFTree(numberOfTests, i, rand.Next(10000)));
            }
        }

        /// <summary>
        /// Not yet implemented. Idea is to update alpha as we are running.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="startAlpha"></param>
        /// <param name="beta"></param>
        /// <param name="numberOfTests"></param>
        /// <param name="updateAlpha"></param>
        public OnlineRandomForest(int t, int startAlpha, float beta, int numberOfTests, bool updateAlpha)
        {
            _t = t;
            _alpha = startAlpha;
            _beta = beta;
            _numberOfTests = numberOfTests;
            _updateAlpha = updateAlpha;
            Random rand = new Random();
            for (int i = 0; i < t; i++)
            {
                _trees.Add(new ORFTree(numberOfTests, i, rand.Next(10000)));
            }
        }

        
        /// <summary>
        /// Trains the forest with one item
        /// </summary>
        /// <param name="itemToTrainWith">The item to train the tree with</param>
        public void Train(Input itemToTrainWith)
        {
            if (_numberOfFeatures == 0) _numberOfFeatures = itemToTrainWith.FeatureCount;
            Parallel.ForEach(_trees, tree =>
            //foreach (Tree tree in Trees)
            {
                int k = tree.Poisson();
                if (k > 0)
                {
                    for (int u = 0; u < k; u++)
                    {
                        ORFNode nodeToUpdate = tree.findLeaf(itemToTrainWith);
                        nodeToUpdate.UpdateNode(itemToTrainWith);
                        if (nodeToUpdate.Count > _alpha && nodeToUpdate.CalculateGain() > _beta)
                        {
                            nodeToUpdate.Split();
                        }
                    }
                }
                else
                {
                    // Do Out Of Bag Error here.
                    tree.Testers.Add(itemToTrainWith);
                    if (tree.NumberOfItems > 10)
                    {
                        foreach (Input inp in tree.Testers)
                        {
                            int prediction = tree.Predict(itemToTrainWith);
                            tree.OutOfBagTested++;
                            if (prediction != itemToTrainWith.Classification)
                                tree.OutOfBagErrors++;
                            if (tree.NumberOfItems > 10 && tree.OutOfBagTested > 10 && tree.OutOfBagErrors / tree.OutOfBagTested > 0.3f)
                            {
                                tree.Reset();
                                tree.TrainTree(itemToTrainWith, 1, _alpha, _beta);
                                break;
                            }
                        }
                        tree.Testers.Clear();
                    }
                }

            });
        }


        /// <summary>
        /// Trains the forest, one at the time in order, with a list of items.
        /// </summary>
        /// <param name="itemsToTrainWith">List of items to train with</param>
        public void Train(List<Input> itemsToTrainWith)
        {
            _numberOfFeatures = itemsToTrainWith.First().FeatureCount;
            Parallel.ForEach(_trees, tree =>
                //foreach (Tree tree in Trees)
                {

                    int i = 0;
                    for (int itemIteration = 0; itemIteration < itemsToTrainWith.Count; itemIteration++)
                    {
                        Input itemToTrainWith = itemsToTrainWith[itemIteration];


                        int k = tree.Poisson();
                        if (k > 0)
                        {
                            tree.TrainTree(itemToTrainWith, k, _alpha, _beta);
                        }
                        else
                        {
                            // Do Out Of Bag Error here.
                            tree.Testers.Add(itemToTrainWith);
                            if (tree.NumberOfItems > 10)
                            {
                                foreach (Input inp in tree.Testers)
                                {
                                    int prediction = tree.Predict(itemToTrainWith);
                                    tree.OutOfBagTested++;
                                    if (prediction != itemToTrainWith.Classification)
                                        tree.OutOfBagErrors++;
                                    if (tree.NumberOfItems > 10 && tree.OutOfBagTested > 10 && tree.OutOfBagErrors / tree.OutOfBagTested > 0.3f)
                                    {
                                        tree.Reset();
                                        tree.TrainTree(itemToTrainWith, 1, _alpha, _beta);
                                        break;
                                    }
                                }
                                
                            }

                        }
                        i++;
                    }
                    tree.Done = true;
                }
                );

        }

        /// <summary>
        /// Predicts the class of itemToPredict.
        /// Picks the class, which has been chosen the most by the trees.
        /// </summary>
        /// <param name="itemToPredict">The item to predict class of</param>
        /// <returns>Class as int</returns>
        public int Predict(Input itemToPredict)
        {
            ConcurrentDictionary<int, int> predictions = new ConcurrentDictionary<int, int>();
            Parallel.ForEach(_trees, tree =>
            //foreach (Tree tree in Trees)
            {
                if (tree.NumberOfItems != 0)
                {
                    int prediction = tree.Predict(itemToPredict);
                    predictions.AddOrUpdate(prediction, 1, (d, k) => k + 1);
                }
            });
            return predictions.First(d => d.Value == predictions.Max(g => g.Value)).Key;
        }

        /// <summary>
        /// Predicts the percentage of each class itemToPredict is over all trees.
        /// </summary>
        /// <param name="itemToPredict">The item to predict class of</param>
        /// <returns>Dictionary with key=class, value=percentage in chosen leaves.</returns>
        public Dictionary<int, float> PredictPercent(Input itemToPredict)
        {
            ConcurrentDictionary<int, float> predictions = new ConcurrentDictionary<int, float>();
            Parallel.ForEach(_trees, tree =>
            //foreach (Tree tree in Trees)
            {
                if (tree.NumberOfItems != 0)
                {
                    Dictionary<int, float> prediction = tree.PredictPercent(itemToPredict);
                    foreach (KeyValuePair<int, float> kvp in prediction)
                    {
                        predictions.AddOrUpdate(kvp.Key, kvp.Value, (d, k) => kvp.Value + k);
                    }
                }
            });
            return predictions.ToDictionary(d => d.Key, d => d.Value / _trees.Count(tree => tree.NumberOfItems != 0));
        }

        private Dictionary<int,float> _variableImportance;
        public Dictionary<int,float> VariableImportance
        {
            get
            {
                if (_variableImportance == null)
                    _variableImportance = CalcVariableImportance();
                return _variableImportance;
            }
        }

        /// <summary>
        /// Calculates the variable importance, by using Breimans algorithm
        /// </summary>
        /// <returns>Dictionary with key=feature, value=importance</returns>
        public Dictionary<int, float> CalcVariableImportance()
        {
            Dictionary<int, float> varImp = new Dictionary<int, float>();

            int untouched = 0;
            int[] touched = new int[_trees.First(d => d.Testers.Count > 0).Testers.First().FeatureCount];

            foreach (ORFTree tree in _trees)
            {
                foreach (Input oob in tree.Testers)
                {
                    if (oob.Classification == tree.Predict(oob))
                        untouched++;
                }
            }

            for (int i = 0; i < touched.Length; i++)
            {
                foreach (ORFTree tree in _trees)
                {
                    foreach (Input oob in tree.Testers.Select(d => new Input(d)))
                    {
                        int take = tree.Rand.Next(0, tree.Testers.Count - 1);
                        oob.Features[i] = tree.Testers.Where(d => d != oob).ToList()[take].Features[i];
                        if (oob.Classification == tree.Predict(oob))
                            touched[i]++;
                    }
                }
                varImp[i] = (float)((untouched - touched[i])) / (float)this._t;
            }

            

            return varImp;
        }

        /// <summary>
        /// Calculates the variable importance, by checking how many times a node has split on a variable.
        /// </summary>
        /// <returns>Dictionary with key=feature, value=importance</returns>
        public Dictionary<int, float> oldCalcVariableImportance()
        {
            Dictionary<int, float> VarImp = new Dictionary<int, float>();
            List<ORFNode> NodesToCheck = new List<ORFNode>();
            int splits = 0;
            foreach (ORFTree T in _trees)
            {
                NodesToCheck.Add(T.Root);
                while (NodesToCheck.Count != 0)
                {

                    ORFNode nodeToCheck = NodesToCheck.First();
                    NodesToCheck.RemoveAt(0);
                    bool nodeHasChildren = false;
                    if (nodeToCheck.LeftChild != null)
                    {
                        NodesToCheck.Add(nodeToCheck.LeftChild);
                        NodesToCheck.Add(nodeToCheck.RightChild);
                        nodeHasChildren = true;
                    }

                    if (nodeHasChildren)
                    {
                        int feature = nodeToCheck.FeatureToCheck;

                        if (VarImp.ContainsKey(feature))
                            VarImp[feature]++;
                        else
                            VarImp.Add(feature, 1);
                        splits++;
                    }
                }
            }
            for (int i = 0; i < _numberOfFeatures; i++)
            {
                if (VarImp.ContainsKey(i))
                    VarImp[i] /= (float)splits;
                else
                    VarImp.Add(i, 0);
            }
            VarImp = VarImp.OrderBy(d => d.Key).ToDictionary(d => d.Key, d => d.Value);
            return VarImp;
        }

        public static double PredictionDifference(Dictionary<int, float> prediction1, Dictionary<int, float> prediction2)
        {
            List<int> integersToCheck = new List<int>();
            foreach (KeyValuePair<int, float> kvp in prediction1)
            {
                if (!integersToCheck.Contains(kvp.Key))
                integersToCheck.Add(kvp.Key);
            }
            foreach (KeyValuePair<int, float> kvp in prediction2)
            {
                if (!integersToCheck.Contains(kvp.Key))
                    integersToCheck.Add(kvp.Key);
            }
            double result = 0;
            foreach (int key in integersToCheck)
            {
                float p1 = prediction1.ContainsKey(key) ? prediction1[key] : 0;
                float p2 = prediction2.ContainsKey(key) ? prediction2[key] : 0;
                result += Math.Abs(p1 - p2);

            }
            return result;
        }

        public byte[] Serialize()
        {
            return new byte[1];
        }

        bool _finalized = false;
        public bool Finalized
        {
            get { return _finalized; }
            set { _finalized = value; }
        }

        
    }

    public class ORFTree
    {
        public ORFNode Root;
        public Random Rand;
        public int NumberOfItems = 0;
        public int Seed = 0;
        public float OutOfBagErrors = 0;
        public float OutOfBagTested = 0;
        public int NumberOfTimesDropped = 0;
        public bool Done = false;

        public List<Input> Testers = new List<Input>();

        int _depth = 0;
        public int Depth { get { return _depth; } set { _depth = value; } }

        int _treeNumber = 0;
        int _numberOfFunctions;

        public ORFTree()
        {
            _numberOfFunctions = 10;
            this.Seed = 10;
            Rand = new Random(Seed);
        }

        public ORFTree(int numberOfFunctions, int treeNumberGiven, int seed)
        {
            Root = new ORFNode(numberOfFunctions, new List<Input>(), this);
            _treeNumber = treeNumberGiven;
            _numberOfFunctions = numberOfFunctions;
            this.Seed = seed;
            Rand = new Random(seed);
        }

        public void Reset()
        {
            NumberOfTimesDropped++;
            NumberOfItems = 0;
            Root = new ORFNode(_numberOfFunctions, new List<Input>(), this);
            _depth = 0;
            OutOfBagErrors = 0;
            OutOfBagTested = 0;
        }

        public void TrainTree(Input itemToTrainWith, int k, float Alpha, float Beta)
        {
            for (int u = 0; u < k; u++)
            {
                NumberOfItems++;
                ORFNode nodeToUpdate = findLeaf(itemToTrainWith);
                nodeToUpdate.UpdateNode(itemToTrainWith);
                if (nodeToUpdate.Count > Alpha && nodeToUpdate.CalculateGain() > Beta)
                {
                    nodeToUpdate.Split();
                }
            }
        }

        public int Predict(Input itemToPredict)
        {
            ORFNode nodeToPredict = findLeaf(itemToPredict);
            return nodeToPredict.Prediction;
        }


        public Dictionary<int, float> PredictPercent(Input itemToPredict)
        {
            Dictionary<int, float> predictions = new Dictionary<int, float>();
            ORFNode nodeToPredict = findLeaf(itemToPredict);
            int count = nodeToPredict.PredictionCount.Sum(d => d.Value);
            predictions = nodeToPredict.PredictionCount.ToDictionary(d => d.Key, d => (float)d.Value / (float)count);
            if (predictions.Sum(d => d.Value) != 1)
                Debug.WriteLine("Sum not equal to 1 in Tree " + _treeNumber);
            return predictions;
        }

        public ORFNode findLeaf(Input itemToTrainWith)
        {
            ORFNode nodeAt = Root;
            ORFNode nodeAtLast;

            do
            {
                nodeAtLast = nodeAt;
                nodeAt = nodeAt.FindChild(itemToTrainWith);
            }
            while (nodeAt != nodeAtLast);
            return nodeAt;

        }

        static readonly double lambda = Math.Exp(-1);
        public int Poisson()
        {
            int k = 0;
            double p = 1;

            do
            {
                k++;
                p *= Rand.NextDouble();
            }
            while (p > lambda);
            return k - 1;
        }


        internal string Serialize()
        {
            return "";
        }
    }

    public class ORFNode
    {
        public int FeatureToCheck;
        public double Threshold;
        public ORFNode LeftChild;
        public ORFNode RightChild;
        
        public List<Input> Items = new List<Input>();
        public int Prediction = 0;
        public Dictionary<int, int> PredictionCount = new Dictionary<int, int>();

        public int Count { get { return Items.Count; } }

        Random _rand = new Random();
        int _numberOfFunctions;
        
        List<Tuple<int, double>> _functionChecks = new List<Tuple<int, double>>();
        
        List<List<Input>> _leftChildren = new List<List<Input>>();
        
        List<List<Input>> _rightChildren = new List<List<Input>>();
        
        List<double> _statistics = new List<double>();
        
        List<int> _numberOfItemsInClassC = new List<int>();
        ORFTree _treePartOf;

        
        int[,] _splitRightStats;
        int[,] splitRightStats
        {
            get
            {
                if (_splitRightStats == null)
                {
                    _splitRightStats = new int[_numberOfFunctions, 100];
                    return _splitRightStats;
                }
                else
                    return _splitRightStats;
            }
        }

        
        int[,] _splitLeftStats;
        int[,] splitLeftStats
        {
            get
            {
                if (_splitLeftStats == null)
                {
                    _splitLeftStats = new int[_numberOfFunctions, 100];
                    return _splitLeftStats;
                }
                else
                    return _splitLeftStats;
            }
        }


        public ORFNode()
        {

        }

        public ORFNode(int numberOfFunctions, List<Input> children, ORFTree treePart)
        {
            _treePartOf = treePart;
            _numberOfFunctions = numberOfFunctions;
            Items = children;
            for (int i = 0; i < numberOfFunctions; i++)
            {
                _leftChildren.Add(new List<Input>());
                _rightChildren.Add(new List<Input>());

                _statistics.Add(0);

                if (children.Count > 1)
                {
                    generateFunctions();
                }

            }
            if (Items.Count > 1)
                makeStatistics(Items);
            else
            {
                if (Items.Count > 0)
                    PredictionCount.Add(Items.First().Classification, 1);
            }
        }

        public ORFNode FindChild(Input itemToFind)
        {
            if (LeftChild == null)
                return this;
            if (itemToFind.GetFeature(FeatureToCheck) < Threshold)
            {
                return LeftChild;
            }
            else
                return RightChild;
        }

        public void UpdateNode(Input itemToInsert)
        {
            Items.Add(itemToInsert);
            Prediction = Items.GroupBy(d => d.Classification).OrderByDescending(d => d.Count()).First().First().Classification;

            if (_functionChecks.Count == 0 && Items.Count > 1 && Items.Select(d => d.Classification).Distinct().Count() > 1)
            {
                for (int i = 0; i < _numberOfFunctions; i++)
                    generateFunctions();
                makeStatistics(Items);
            }
            else
                makeStatistics(itemToInsert);
        }

        public double CalculateGain()
        {
            if (_leftChildren.Count == 0 || _rightChildren.Count == 0)
                return 0;
            int K = _numberOfItemsInClassC.Count;
            if (_functionChecks.Count == 0)
                return 0;
            for (int i = 0; i < _numberOfFunctions; i++)
            {
                double gain = 0;
                double LRj = 0;
                double LRjls = 0;
                double LRjrs = 0;
                double RjSize = Items.Count;
                double RjlsSize = _leftChildren[i].Count;
                double RjrsSize = _rightChildren[i].Count;

                Tuple<int, double> func = _functionChecks[i];

                for (int c = 0; c < K; c++)
                {
                    double numberInClassC = _numberOfItemsInClassC[c] / RjSize;
                    LRj += numberInClassC * (1 - numberInClassC);

                    double leftSideC = (double)splitLeftStats[i, c] / RjSize;
                    double rightSideC = (double)splitRightStats[i, c] / RjSize;

                    LRjls += leftSideC * (1 - leftSideC);
                    LRjrs += rightSideC * (1 - rightSideC);

                }
                gain = LRj - (RjlsSize / RjSize) * LRjls - (RjrsSize / RjSize) * LRjrs;
                _statistics[i] = gain;

            }
            return _statistics.Max();
        }

        public void Split()
        {
            double max = _statistics.Max();
            List<int> indexesOfMax = new List<int>();
            for (int i = 0; i < _statistics.Count; i++)
            {
                if (_statistics[i] == _statistics.Max())
                    indexesOfMax.Add(i);
            }
            int bestIndex = indexesOfMax[_rand.Next(0, indexesOfMax.Count)];
            LeftChild = new ORFNode(_numberOfFunctions, _leftChildren[bestIndex], _treePartOf);
            LeftChild.Prediction = LeftChild.Items.GroupBy(d => d.Classification).OrderByDescending(d => d.Count()).First().First().Classification;
            RightChild = new ORFNode(_numberOfFunctions, _rightChildren[bestIndex], _treePartOf);
            RightChild.Prediction = RightChild.Items.GroupBy(d => d.Classification).OrderByDescending(d => d.Count()).First().First().Classification;
            _treePartOf.Depth++;
            FeatureToCheck = _functionChecks[bestIndex].Item1;
            Threshold = _functionChecks[bestIndex].Item2;


            Items.Clear();
            _statistics.Clear();
            _functionChecks.Clear();
            _leftChildren.Clear();
            _rightChildren.Clear();
            _numberOfItemsInClassC.Clear();
            PredictionCount.Clear();
            _splitLeftStats = null;
            _splitRightStats = null;
        }

        private void generateFunctions()
        {

            int featureToCheckFunc = _treePartOf.Rand.Next(0, Items.First().FeatureCount);

            double minValue = Items.Min(d => d.Features[featureToCheckFunc]);
            double maxValue = Items.Max(d => d.Features[featureToCheckFunc]);
            _functionChecks.Add(new Tuple<int, double>(featureToCheckFunc, _treePartOf.Rand.NextDouble() * (maxValue - minValue) + minValue));
        }

        private void makeStatistics(List<Input> children)
        {
            foreach (Input child in children)
            {
                if (PredictionCount.ContainsKey(child.Classification))
                    PredictionCount[child.Classification]++;
                else
                    PredictionCount.Add(child.Classification, 1);
                for (int i = 0; i < _numberOfFunctions; i++)
                {
                    Tuple<int, double> func = _functionChecks[i];

                    if (child.Features[func.Item1] < func.Item2)
                    {
                        this.splitLeftStats[i, child.Classification]++;
                        _leftChildren[i].Add(child);
                    }
                    else
                    {
                        this.splitRightStats[i, child.Classification]++;
                        _rightChildren[i].Add(child);
                    }
                }
                if (!(_numberOfItemsInClassC.Count > child.Classification))
                    while (_numberOfItemsInClassC.Count <= child.Classification)
                        _numberOfItemsInClassC.Add(0);
                _numberOfItemsInClassC[child.Classification]++;

            }
        }

        private void makeStatistics(Input child)
        {
            if (PredictionCount.ContainsKey(child.Classification))
                PredictionCount[child.Classification]++;
            else
                PredictionCount.Add(child.Classification, 1);
            if (_functionChecks.Count == 0)
                return;
            for (int i = 0; i < _numberOfFunctions; i++)
            {
                Tuple<int, double> func = _functionChecks[i];
                if (child.Features[func.Item1] < func.Item2)
                {
                    this.splitLeftStats[i, child.Classification]++;
                    _leftChildren[i].Add(child);
                }
                else
                {
                    this.splitRightStats[i, child.Classification]++;
                    _rightChildren[i].Add(child);
                }
            }
            if (!(_numberOfItemsInClassC.Count > child.Classification))
                while (_numberOfItemsInClassC.Count <= child.Classification)
                    _numberOfItemsInClassC.Add(0);
            _numberOfItemsInClassC[child.Classification]++;
        }

    }
}
