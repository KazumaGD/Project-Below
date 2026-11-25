namespace ProjectBelow.Narrative
{
    internal sealed class DialogueConversation
    {
        private readonly DialogueGraph _graph;

        internal DialogueDefinition.Node Current { get; private set; }
        internal int Step { get; private set; }

        internal DialogueConversation(DialogueGraph graph)
        {
            _graph = graph;
            Current = graph.Entry;
        }

        internal bool TryContinue(int step)
        {
            if (Current == null || Step != step || Current.Choices.Count != 0)
                return false;

            Follow(Current.NextNodeId);
            return true;
        }

        internal bool TryChoose(int step, int index, out string flagId)
        {
            flagId = null;
            if (Current == null || Step != step || index < 0 || index >= Current.Choices.Count)
                return false;

            var choice = Current.Choices[index];
            flagId = choice.FlagId;
            Follow(choice.NextNodeId);
            return true;
        }

        private void Follow(string id)
        {
            Current = _graph.Resolve(id);
            Step++;
        }
    }
}
