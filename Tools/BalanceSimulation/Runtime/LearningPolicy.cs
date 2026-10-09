// Optional external actor policy. Weights remain frozen for the entire campaign.
object ReadAgentJson(string json, System.Type type) {
    var converter = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert", true);
    return converter.GetMethod("DeserializeObject", new[] { typeof(string), typeof(System.Type) }).Invoke(null, new object[] { json, type });
}
System.Collections.Generic.Dictionary<string,object> ReadAgentModel(string path) {
    var model = (System.Collections.Generic.Dictionary<string,object>)ReadAgentJson(System.IO.File.ReadAllText(path), typeof(System.Collections.Generic.Dictionary<string,object>));
    if ((string)model["schema"] != "balance-actor-v1" || System.Convert.ToInt32(model["featureVersion"]) != 1)
        throw new System.InvalidOperationException("Incompatible agent model.");
    return model;
}
double[] AgentWeights(System.Collections.Generic.Dictionary<string,object> model) =>
    (double[])ReadAgentJson(model["weights"].ToString(), typeof(double[]));
System.Collections.Generic.Dictionary<string,double> AgentOffsets(System.Collections.Generic.Dictionary<string,object> model) =>
    (System.Collections.Generic.Dictionary<string,double>)ReadAgentJson(model["offsets"].ToString(), typeof(System.Collections.Generic.Dictionary<string,double>));
double AgentLogit(double prior, double[] features, double[] weights, string key, System.Collections.Generic.Dictionary<string,double> offsets) {
    if (features.Length != weights.Length) throw new System.InvalidOperationException("Agent feature dimensions do not match.");
    double result = System.Math.Log(1 + System.Math.Max(0, prior));
    for (int i = 0; i < features.Length; i++) result += features[i] * weights[i];
    if (offsets.TryGetValue(key, out var offset)) result += offset;
    if (double.IsNaN(result) || double.IsInfinity(result)) throw new System.InvalidOperationException("Non-finite actor score.");
    return result;
}
int AgentSelect(double[] logits, System.Random random, bool training, double temperature, double exploration, out double[] softmax, out double[] probabilities) {
    if (logits.Length == 0 || temperature <= 0 || exploration < 0 || exploration > 1) throw new System.ArgumentOutOfRangeException("Actor settings");
    double maximum = logits.Max(); softmax = logits.Select(v => System.Math.Exp(System.Math.Max(-60, (v - maximum) / temperature))).ToArray();
    double total = softmax.Sum(); for (int i = 0; i < softmax.Length; i++) softmax[i] /= total;
    probabilities = softmax.Select(p => (1 - exploration) * p + exploration / logits.Length).ToArray();
    if (!training) return System.Array.IndexOf(logits, maximum);
    double roll = random.NextDouble(); for (int i = 0; i < probabilities.Length; i++) { roll -= probabilities[i]; if (roll <= 0) return i; }
    return probabilities.Length - 1;
}
