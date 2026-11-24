using System.Collections.Generic;
using UnityEngine;

namespace VirtualArchery
{
    // Owned by one score session. Results retain it after scoring stops; retries create a new instance.
    public sealed class PracticeShotSummary
    {
        readonly Target practiceTarget;
        readonly List<PracticeDistanceShots> groups = new List<PracticeDistanceShots>();

        public IReadOnlyList<PracticeDistanceShots> Groups => groups;
        public float CurrentDistance { get; private set; }
        public int UnplottedShots { get; private set; }

        internal PracticeShotSummary(Target target)
        {
            practiceTarget = target;
            SetCurrentDistance(target.transform.position.z);
        }

        internal void SetCurrentDistance(float distance)
        {
            CurrentDistance = Mathf.Round(distance * 10f) / 10f;
        }

        internal void Record(Target target, Vector3 worldPoint)
        {
            if (target == practiceTarget && target != null && target.radius > 0f)
            {
                // Retargeting is locked until every airborne shot resolves. Read the real target
                // position here, and never alter a populated group's recorded distance afterward.
                SetCurrentDistance(target.transform.position.z);
                PracticeDistanceShots group = null;
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].Distance != CurrentDistance) continue;
                    group = groups[i];
                    break;
                }
                if (group == null)
                {
                    group = new PracticeDistanceShots(CurrentDistance);
                    groups.Add(group);
                }
                Vector3 local = target.transform.InverseTransformPoint(worldPoint);
                group.Add(new Vector2(local.x / target.radius, local.y / target.radius));
                return;
            }
            // A backstop/ground collision or timeout does not reveal a target-face impact.
            UnplottedShots++;
        }
    }

    public sealed class PracticeDistanceShots
    {
        readonly List<Vector2> impacts = new List<Vector2>();
        Vector2 centroid;
        float squaredSpread;

        public float Distance { get; }
        public IReadOnlyList<Vector2> Impacts => impacts;
        public Vector2 Centroid => centroid;
        public float Spread => impacts.Count == 0 ? 0f : Mathf.Sqrt(Mathf.Max(0f, squaredSpread / impacts.Count));

        internal PracticeDistanceShots(float distance)
        {
            Distance = distance;
        }

        internal void Add(Vector2 impact)
        {
            impacts.Add(impact);
            // Online variance avoids cancellation when many shots form a very tight off-centre group.
            Vector2 delta = impact - centroid;
            centroid += delta / impacts.Count;
            squaredSpread += Vector2.Dot(delta, impact - centroid);
        }
    }
}
