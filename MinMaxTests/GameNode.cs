using Newtonsoft.Json;

namespace MinMaxTests
{
    internal class Node<T> where T : class
    {
        public Node()
        {
            this.Children = new List<Node<T>>();
            Value = default(T);
        }

        public Node(T _value, List<Node<T>>? children = null)
        {
            Value = _value;
            Children = children;
        }

        public void AddChild(Node<T> node)
        {
            if (Children == null)
            {
                Children = new List<Node<T>>();
            }
            Children.Add(node);
        }

        public static Node<T>? ReadFromJson(string json)
        {
            return JsonConvert.DeserializeObject<Node<T>>(json);
        }

        [JsonProperty("value")]
        public T? Value { get; set; }

        [JsonProperty("children", NullValueHandling = NullValueHandling.Ignore)]
        public List<Node<T>>? Children { get; set; }

        [JsonIgnore]
        public string JSon => JsonConvert.SerializeObject(this);
    }
}
