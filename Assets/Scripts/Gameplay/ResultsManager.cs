using UnityEngine;

namespace VirtualArchery
{
    public class ResultsManager : MonoBehaviour
    {
        public bool IsCompetitive { get; private set; }
        public bool Won { get; private set; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int Points { get; private set; }
        public float Accuracy { get; private set; }
        public string Outcome => !IsCompetitive ? "Practice complete" :
            Won ? "Challenge won!" : "Challenge complete - goal not reached";

        public void Capture(bool competitive, bool won, ScoreManager score)
        {
            IsCompetitive = competitive;
            Won = competitive && won;
            Shots = score.Shots;
            Hits = score.Hits;
            Misses = score.Misses;
            Points = score.Points;
            Accuracy = score.Accuracy;
        }
    }
}
