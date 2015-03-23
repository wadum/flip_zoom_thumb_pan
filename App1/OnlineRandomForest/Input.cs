using App1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Json;

namespace OnlineRandomForest
{
    public class Input
    {
        public string Id { get; set; }
        public string IFeatures
        {
            set
            {
                byte[] bytes = Convert.FromBase64String(value);
                this.Features = JsonConvert.DeserializeObject<List<double>>(ue.GetString(bytes, 0, bytes.Length));
            }
            get
            {
                return Convert.ToBase64String(ue.GetBytes(JsonConvert.SerializeObject(this.Features)));
            }
        }
        public int Classification { get; set; }

        [JsonIgnore]
        public List<double> Features { get; set; }
        [JsonIgnore]
        public List<double> FeaturesNormalized { get; set; }
        [JsonIgnore]
        private UnicodeEncoding ue = new UnicodeEncoding();
        

        public Input() {}

        public Input(Input inp)
        {
            AddFeatureRange(inp.Features);
            FeaturesNormalized.AddRange(inp.FeaturesNormalized);
            Classification = inp.Classification;
        }

        public double Distance(Input other, bool normalize = false)
        {
            double distance = 0;
            for (int i = 0; i < FeatureCount; i++)
            {
                double dist = normalize ? this.FeaturesNormalized[i] - other.FeaturesNormalized[i] : this.GetFeature(i) - other.GetFeature(i);
                distance += dist * dist;
            }
            return Math.Sqrt(distance);
        }


        public double ErrorDistance(Input other)
        {
            double distance = 0;
            for (int i = 0; i < FeatureCount; i++)
            {
                double dist = Math.Abs(this.GetFeature(i) - other.GetFeature(i));
                distance += Math.Max((Math.Log(dist / 0.1, 2)), 1);
            }
            return distance;
        }

        public int FeatureCount { get { return Features.Count; } }

        public double GetFeature(int featureNumber)
        {
            return Features[featureNumber];
        }

        public void AddFeature(double feature)
        {
            this.Features.Add(feature);
        }

        public void AddFeature(float feature)
        {
            AddFeature((double)feature);
        }

        public void AddFeatureRange(List<double> features)
        {
            Features.AddRange(features);
        }
    }
}
