namespace AionSpeedrunOverlay.Quest
{
    public enum QuestDisplaySafetyAction
    {
        Hold,
        Clear
    }


    public sealed class QuestDisplaySafetyPolicy
    {
        public QuestDisplaySafetyAction Observe(
            QuestMatch match,
            string displayedRecognitionId)
        {
            // Eine bestätigte Notiz bleibt sichtbar, bis ein anderer Zustand
            // selbst die normale Mehrfachbestätigung bestanden hat. Weder
            // OCR-Lücken noch ein einzelner widersprechender Treffer dürfen
            // die Anzeige dazwischen leeren und Flackern erzeugen.
            return QuestDisplaySafetyAction.Hold;
        }


        public void Reset()
        {
        }
    }
}
