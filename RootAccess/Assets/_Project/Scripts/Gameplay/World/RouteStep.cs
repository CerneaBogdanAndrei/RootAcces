using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>How the intended route crosses one waypoint (see <see cref="RouteStep"/>).</summary>
    public enum RouteAction
    {
        /// <summary>Run to the waypoint (hopping over small steps).</summary>
        Walk,
        /// <summary>Jump from where the player stands to the waypoint (double jump / dash as needed).</summary>
        Jump,
        /// <summary>Climb a chimney wall to wall up to the waypoint.</summary>
        WallClimb,
        /// <summary>Crouch through a low duct to the waypoint.</summary>
        Crawl,
        /// <summary>Use the interactable at the waypoint (terminal, node, elevator, exit); solve the hack if one opens.</summary>
        Interact,
        /// <summary>Board the moving platform (gondola, elevator) and ride it to the waypoint.</summary>
        Ride,
        /// <summary>Swim (diving where needed) to the waypoint and climb out.</summary>
        Swim,
        /// <summary>Let the fan's updraft lift the player, then drift onto the waypoint.</summary>
        Updraft,
        /// <summary>Hold the area around the waypoint until the subject (the broadcast) is done.</summary>
        Hold
    }

    /// <summary>
    /// One waypoint of a level's intended route, written by the level generator next to the segment it belongs to. The
    /// route is data only: the game never reads it. It documents how every segment is meant to be crossed (gizmos in the
    /// Scene view) and the automated playthrough (<c>-ra-playthrough</c>) follows it with real input.
    /// </summary>
    public sealed class RouteStep : MonoBehaviour
    {
        [SerializeField] private int order;
        [SerializeField] private RouteAction action;
        [SerializeField, Tooltip("Segment of the level this step belongs to (for reports).")]
        private string segment;
        [SerializeField, Tooltip("Terminal, node, elevator, gondola, fan or transmitter the step works with.")]
        private MonoBehaviour subject;
        [SerializeField, Tooltip("Take a screenshot here during the automated playthrough.")]
        private bool screenshot;

        public int Order => order;
        public RouteAction Action => action;
        public string Segment => segment;
        public MonoBehaviour Subject => subject;
        public bool Screenshot => screenshot;
        public Vector2 Position => transform.position;

        public void Configure(int index, RouteAction routeAction, string segmentName, MonoBehaviour routeSubject, bool takeScreenshot)
        {
            order = index;
            action = routeAction;
            segment = segmentName;
            subject = routeSubject;
            screenshot = takeScreenshot;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = action switch
            {
                RouteAction.Interact => Color.yellow,
                RouteAction.Jump or RouteAction.WallClimb or RouteAction.Updraft => Color.cyan,
                RouteAction.Ride or RouteAction.Hold => Color.magenta,
                _ => Color.green
            };
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);
        }
    }
}
