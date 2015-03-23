using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineRandomForest
{
    public enum RandomForestType { RandomForest = 0, OnlineRandomForest = 1, HeirarchicalRF = 2, HeirarchicalORF = 3, BinaryHeiarchicalORF = 4, BinaryORF = 5, KNN = 6, SimpleClassifier = 7, SVM = 8 }

    public class Wrapper
    {
        public IRandomForest RandomForest { get; set; }
    }

    public interface IRandomForest
    {
        void Train(List<Input> itemsToTrainWith);

        int Predict(Input itemToPredict);

        Dictionary<int, float> PredictPercent(Input itemToPredict);

        Dictionary<int, float> VariableImportance
        {
            get;
        }

        Dictionary<int, float> CalcVariableImportance();

        bool Done
        {
            get;
        }

        float PercentDone
        {
            get;
        }

        RandomForestType ForestType
        {
            get;
        }

        byte[] Serialize();

        bool Finalized
        {
            get;
            set;
        }
    }
}
