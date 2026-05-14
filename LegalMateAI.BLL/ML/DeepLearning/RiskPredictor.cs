using Microsoft.ML;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.ML.DeepLearning
{
    public class RiskPredictor
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;
        private readonly string _modelPath;

        public RiskPredictor()
        {
            _mlContext = new MLContext(seed: 42);
            _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "ML_Models", "risk_predictor.zip");
        }

        public async Task TrainAsync(List<RiskTrainingData> trainingData)
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            
            var pipeline = _mlContext.Transforms.Concatenate("Features", 
                    "TextLength", "HasNumbers", "HasSpecialChars", "WordCount")
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", "RiskLevel"))
                .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy())
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _model = pipeline.Fit(dataView);
            
            var directory = Path.GetDirectoryName(_modelPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);
                
            _mlContext.Model.Save(_model, dataView.Schema, _modelPath);
            await Task.CompletedTask;
        }

        public RiskPrediction Predict(RiskTrainingData features)
        {
            if (_model == null && File.Exists(_modelPath))
                _model = _mlContext.Model.Load(_modelPath, out _);

            if (_model == null) return new RiskPrediction { PredictedLabel = "Medium" };

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<RiskTrainingData, RiskPrediction>(_model);
            return predictionEngine.Predict(features);
        }
    }

    public class RiskTrainingData
    {
        public float TextLength { get; set; }
        public float HasNumbers { get; set; }
        public float HasSpecialChars { get; set; }
        public float WordCount { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
    }

    public class RiskPrediction
    {
        public string PredictedLabel { get; set; } = string.Empty;
        public float Probability { get; set; }
    }
}