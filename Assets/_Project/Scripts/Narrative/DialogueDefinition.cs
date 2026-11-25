using System;
using System.Collections.Generic;
using ProjectBelow.Hub;
using UnityEngine;

namespace ProjectBelow.Narrative
{
    [CreateAssetMenu(menuName = "Project Below/Dialogue Definition")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Node
        {
            [SerializeField] private string _id;
            [SerializeField] private CharacterDefinition _speaker;
            [SerializeField, TextArea(2, 5)] private string _text;
            [SerializeField] private string _nextNodeId;
            [SerializeField] private Choice[] _choices;

            public string Id => _id;
            public CharacterDefinition Speaker => _speaker;
            public string Text => _text;
            public string NextNodeId => _nextNodeId;
            public IReadOnlyList<Choice> Choices => _choices ?? Array.Empty<Choice>();
        }

        [Serializable]
        public sealed class Choice
        {
            [SerializeField] private string _text;
            [SerializeField] private string _nextNodeId;
            [SerializeField] private string _flagId;

            public string Text => _text;
            public string NextNodeId => _nextNodeId;
            public string FlagId => _flagId;
        }

        [SerializeField] private string _entryNodeId;
        [SerializeField] private Node[] _nodes;

        public bool Validate(out string error) => TryBuild(out _, out error);

        internal bool TryBuild(out DialogueGraph graph, out string error)
        {
            graph = null;
            var nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
            foreach (var node in _nodes ?? Array.Empty<Node>())
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || node.Id != node.Id.Trim())
                    return Fail("Every node needs a nonempty ID without leading or trailing spaces.", out error);
                if (!nodes.TryAdd(node.Id, node))
                    return Fail($"Duplicate node ID '{node.Id}'.", out error);
                if (node.Speaker == null || string.IsNullOrWhiteSpace(node.Text))
                    return Fail($"Node '{node.Id}' needs a speaker and text.", out error);
                if (node.Choices.Count > 0 && !string.IsNullOrEmpty(node.NextNodeId))
                    return Fail($"Node '{node.Id}' has both a continue target and choices.", out error);

                foreach (var choice in node.Choices)
                    if (choice == null || string.IsNullOrWhiteSpace(choice.Text)
                        || (!string.IsNullOrEmpty(choice.FlagId) && !StoryFlags.IsValidId(choice.FlagId)))
                        return Fail($"Node '{node.Id}' has an invalid choice or flag ID.", out error);
            }

            if (string.IsNullOrEmpty(_entryNodeId) || !nodes.TryGetValue(_entryNodeId, out var entry))
                return Fail("The entry node does not exist.", out error);

            var predecessors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var exits = new Stack<string>();
            foreach (var node in nodes.Values)
            {
                foreach (var target in Targets(node))
                {
                    if (string.IsNullOrEmpty(target))
                    {
                        exits.Push(node.Id);
                        continue;
                    }
                    if (!nodes.ContainsKey(target))
                        return Fail($"Node '{node.Id}' points to missing node '{target}'.", out error);
                    if (!predecessors.TryGetValue(target, out var incoming))
                        predecessors[target] = incoming = new List<string>();
                    incoming.Add(node.Id);
                }
            }

            // Follow edges backwards from the exits so a branch with no way out cannot trap the Hub.
            var canFinish = new HashSet<string>(StringComparer.Ordinal);
            while (exits.TryPop(out var id))
                if (canFinish.Add(id) && predecessors.TryGetValue(id, out var incoming))
                    foreach (var predecessor in incoming)
                        exits.Push(predecessor);

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var participants = new List<CharacterDefinition>();
            var speakers = new HashSet<CharacterDefinition>();
            var pending = new Stack<Node>();
            pending.Push(entry);
            while (pending.TryPop(out var node))
            {
                if (!visited.Add(node.Id))
                    continue;
                if (!canFinish.Contains(node.Id))
                    return Fail($"Node '{node.Id}' has no path to the end.", out error);
                if (speakers.Add(node.Speaker))
                    participants.Add(node.Speaker);
                foreach (var target in Targets(node))
                    if (!string.IsNullOrEmpty(target))
                        pending.Push(nodes[target]);
            }

            graph = new DialogueGraph(entry, nodes, participants.AsReadOnly());
            error = null;
            return true;
        }

        private static IEnumerable<string> Targets(Node node)
        {
            if (node.Choices.Count == 0)
                yield return node.NextNodeId;
            else
                foreach (var choice in node.Choices)
                    yield return choice.NextNodeId;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }

    internal sealed class DialogueGraph
    {
        private readonly Dictionary<string, DialogueDefinition.Node> _nodes;
        internal DialogueDefinition.Node Entry { get; }
        internal IReadOnlyList<CharacterDefinition> Participants { get; }

        internal DialogueGraph(DialogueDefinition.Node entry, Dictionary<string, DialogueDefinition.Node> nodes,
            IReadOnlyList<CharacterDefinition> participants)
        {
            Entry = entry;
            _nodes = nodes;
            Participants = participants;
        }

        internal DialogueDefinition.Node Resolve(string id) => string.IsNullOrEmpty(id) ? null : _nodes[id];
    }
}
