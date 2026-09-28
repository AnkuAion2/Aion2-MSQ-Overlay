namespace AionSpeedrunOverlay.Quest
{
    public static class QuestRecognitionConfirmationPolicy
    {
        public static int GetRequiredConfirmations(QuestMatch match)
        {
            if (match.Step != null)
            {
                // A weak but plausible step survived two frames during the
                // level-40 run and briefly exposed the following route note.
                // Strong steps need three observations; weaker steps need a
                // fourth before they can replace the visible instruction.
                return match.StepScore >= 0.95 ? 3 : 4;
            }

            bool strongQuestMatch =
                match.LevelFilterApplied &&
                match.Score >= 0.85;

            return strongQuestMatch ? 2 : 3;
        }
    }
}
