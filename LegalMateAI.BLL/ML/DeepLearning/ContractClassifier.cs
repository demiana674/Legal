using Microsoft.ML;
using Microsoft.ML.Transforms.Text;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.ML.DeepLearning
{
    public class ContractClassifier
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;
        private readonly string _modelPath;
        private readonly ILogger<ContractClassifier>? _logger;

        public ContractClassifier(ILogger<ContractClassifier>? logger = null)
        {
            _mlContext = new MLContext(seed: 42);
            _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "ML_Models", "contract_classifier.zip");
            _logger = logger;
        }

        public async Task TrainAsync(List<ContractTrainingData> trainingData)
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            
            var pipeline = _mlContext.Transforms.Text.NormalizeText("NormalizedText", "Text")
                .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"))
                .Append(_mlContext.Transforms.Text.RemoveDefaultStopWords("FilteredTokens", "Tokens"))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", "ContractType"))
                .Append(_mlContext.Transforms.Text.ProduceNgrams("Features", "FilteredTokens"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _model = pipeline.Fit(dataView);
            
            var directory = Path.GetDirectoryName(_modelPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);
                
            _mlContext.Model.Save(_model, dataView.Schema, _modelPath);
            await Task.CompletedTask;
        }

        public string Predict(string contractText)
        {
            if (_model == null && File.Exists(_modelPath))
                _model = _mlContext.Model.Load(_modelPath, out _);

            if (_model == null) return "Unknown";

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ContractTrainingData, ContractPrediction>(_model);
            var prediction = predictionEngine.Predict(new ContractTrainingData { Text = contractText });
            
            return prediction.PredictedLabel;
        }
    }

    public class ContractTrainingData
    {
        public string Text { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
    }

    public class ContractPrediction
    {
        public string PredictedLabel { get; set; } = string.Empty;
        public float[] Score { get; set; } = Array.Empty<float>();
    }
}