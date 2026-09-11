using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.UI;

namespace VoarVR.World
{
    // A single chapter owns five bounded opaque renderers. Presentation never awards
    // an objective: the collectible stays at the same logical position as its sweep.
    public sealed class JourneyPresentation : MonoBehaviour
    {
        private WorldSpace space;
        private ExpeditionDirector director;
        private BirdFlightDriver bird;
        private WindField wind;
        private Transform root,seed,carrier,crown,guide,targetMarker;
        private Mesh seedMesh,budMesh,bloomMesh,guideMesh,liftMarkerMesh,seedMarkerMesh,archMarkerMesh,landingMarkerMesh;
        private MeshFilter crownFilter,targetFilter;
        private bool hasState,wasRestored;
        private float restoredAt,lastClock;
        private FlightChallenge observedChallenge;
        private bool observedSeed;
        private ObjectiveKind observedKind;
        private Vector3 previousPlayer,pickupOrigin;
        private float pickupAt=-10;
        private const float PickupTransferSeconds=.9f;
        private static readonly Color WayGold=new Color(1,.78f,.12f);
        private static readonly Color WayDark=new Color(.12f,.13f,.09f);
        public bool SeedVisible=>seed!=null && seed.gameObject.activeSelf;
        public bool BloomVisible=>crown!=null && crown.gameObject.activeSelf && wasRestored;
        public bool GuideVisible=>guide!=null && guide.gameObject.activeSelf;
        public bool TargetMarkerVisible=>targetMarker!=null && targetMarker.gameObject.activeSelf;
        public ObjectiveKind TargetMarkerKind { get; private set; }
        public Vector3 TargetMarkerWorldPosition=>targetMarker!=null?targetMarker.position:Vector3.zero;
        public bool NavigationAvailable { get; private set; }
        public LogicalPosition NavigationTarget { get; private set; }
        public bool CarryVisible=>carrier!=null && carrier.gameObject.activeSelf;
        public Vector3 CarryWorldPosition=>carrier!=null?carrier.position:Vector3.zero;
        public bool PickupTransferActive=>CarryVisible && lastClock-pickupAt>=0 && lastClock-pickupAt<PickupTransferSeconds;
        public Vector3 SeedWorldPosition=>seed!=null?seed.position:Vector3.zero;
        public Vector3 CrownWorldPosition=>crown!=null?crown.position:Vector3.zero;
        public Transform CrownAnchor=>crown;

        public void Configure(WorldSpace coordinates,ExpeditionDirector expedition)
        {
            if(root!=null || coordinates==null)return;
            space=coordinates;director=expedition;bird=expedition!=null?expedition.GetComponent<BirdFlightDriver>():null;
            wind=FindAnyObjectByType<WindField>();
            var kit=Resources.Load<SkywardKit>("SkywardKit");if(kit==null)return;
            // Do not parent world cues to the flying bird, even when this component is.
            root=new GameObject("A Route Home presentation").transform;root.SetParent(space.transform,false);
            seedMesh=BuildSeed();budMesh=BuildCrown(false);bloomMesh=BuildCrown(true);guideMesh=BuildGuide();
            liftMarkerMesh=BuildTarget(ObjectiveKind.DiscoverLift);seedMarkerMesh=BuildTarget(ObjectiveKind.CollectSeed);
            archMarkerMesh=BuildTarget(ObjectiveKind.Precision);landingMarkerMesh=BuildTarget(ObjectiveKind.Land);
            seed=Visual("Story seed",seedMesh,kit.Material);carrier=Visual("Carried story seed",seedMesh,kit.Material);
            crown=Visual("Sanctuary crown",budMesh,kit.Material);crownFilter=crown.GetComponent<MeshFilter>();
            guide=Visual("Gold route chevrons",guideMesh,kit.Material);
            targetMarker=Visual("Current route target",liftMarkerMesh,kit.Material);targetFilter=targetMarker.GetComponent<MeshFilter>();
            seed.gameObject.SetActive(false);carrier.gameObject.SetActive(false);guide.gameObject.SetActive(false);targetMarker.gameObject.SetActive(false);
            space.Rebased+=Rebase;
        }

        private Transform Visual(string name,Mesh mesh,Material material)
        {
            var item=new GameObject(name);item.transform.SetParent(root,false);
            item.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=item.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return item.transform;
        }

        public void Tick(Vector3 player)
        {
            if(director==null)return;
            var controller=bird!=null?bird.Controller:null;
            float clock=controller!=null?controller.SimulationTime:director.Challenge?.Elapsed??0;
            bool available=controller!=null && controller.State.Phase!=FlightPhase.Paused && !controller.StreamingBlocked
                && (!bird.UsesXR || bird.Calibration.Captured);
            bool guides=FlightPreferences.GuidanceEnabled && available;
            var challenge=director.Challenge;
            Present(player,challenge,director.Journey!=null && director.Journey.GardenRestored,guides,clock);
        }

        // Also used by explicit editor evidence and tests; no progression is changed.
        public void Present(Vector3 player,FlightChallenge challenge,bool restored,bool guidanceEnabled,float clock)
        {
            if(root==null || !isActiveAndEnabled)return;
            if(!root.gameObject.activeSelf)root.gameObject.SetActive(true);
            if(hasState && clock<lastClock)restoredAt=clock-4;
            bool chapter=challenge!=null && challenge.Activity==FlightActivity.RouteHome;
            bool active=chapter && challenge.Status==ChallengeStatus.Active;
            var pickup=RouteHomeChapter.SeedPosition;
            seed.position=space.ToLocal(pickup.X,pickup.Y,pickup.Z);
            seed.rotation=Quaternion.Euler(0,clock*13,0);
            seed.gameObject.SetActive(active && challenge.Kind==ObjectiveKind.CollectSeed && !challenge.SeedCollected && Vector3.Distance(player,seed.position)<850);

            carrier.gameObject.SetActive(active && challenge.SeedCollected);
            // Attach ahead of the authored eyes, in body space. The Dragon's eyes
            // are farther forward than the small birds'; a fixed .5 m offset put
            // its carried seed behind the player. No camera or tracked-head follow.
            var carryOffset=(bird!=null?bird.ImmersiveEyeAnchor:new Vector3(0,.288f,.32f))+new Vector3(-.24f,-.22f,.90f);
            var bodyRotation=bird!=null?bird.transform.rotation:Quaternion.identity;
            var carryDestination=player+bodyRotation*carryOffset;
            // Only an observed, continuous real pickup animates. A resumed checkpoint,
            // new challenge, clock reset or relocation shows its already-held seed directly.
            bool continuous=observedChallenge==challenge && clock>lastClock && clock-lastClock<=.5f
                && Vector3.Distance(player,previousPlayer)<25;
            if(active && challenge.SeedCollected && !observedSeed && observedKind==ObjectiveKind.CollectSeed && continuous)
            {pickupAt=clock;pickupOrigin=seed.position;}
            else if(!continuous || !active || !challenge.SeedCollected)pickupAt=-10;
            float transfer=Mathf.Clamp01((clock-pickupAt)/PickupTransferSeconds);
            carrier.position=Vector3.Lerp(pickupOrigin,carryDestination,Mathf.SmoothStep(0,1,transfer));
            carrier.rotation=bodyRotation*Quaternion.Euler(0,0,20);
            // One smooth settling pulse, not a repeated flash. The seed stays attached
            // to the creature instead of spinning like the optional airborne moths.
            // Shrink before approaching the eyes: proportional shrinking and motion
            // kept the pickup filling the view halfway through its transfer.
            float carryScale=.035f;
            if(bird!=null && bird.ViewMode==FlightViewMode.ThirdPerson)
            {
                // The Dragon's chase camera sits farther back for its broad wings.
                // Keep possession legible there without enlarging the first-person seed.
                float chase=2.35f+Mathf.Max(0,bird.CharacterRestHalfSpan-.56f)*1.7f;
                carryScale*=Mathf.Clamp((chase+carryOffset.z)/3.5f,1,3);
            }
            carrier.localScale=Vector3.one*Mathf.Lerp(1,carryScale,Mathf.SmoothStep(0,1,Mathf.Clamp01(transfer*2.5f)));
            observedChallenge=challenge;observedSeed=chapter && challenge.SeedCollected;
            observedKind=chapter?challenge.Kind:default;previousPlayer=player;lastClock=clock;

            var garden=FlightRegions.Island(space.Seed,0,0);
            crown.position=space.ToLocal(garden.X,garden.Y,garden.Z)+SkyArchipelago.SanctuaryCrownOffset;
            // Hide the one fixed crown when its pooled island is no longer represented.
            var observer=space.ToLogical(player);
            bool gardenLoaded=System.Math.Abs(System.Math.Floor(observer.X/FlightRegions.IslandCell))<=1
                && System.Math.Abs(System.Math.Floor(observer.Z/FlightRegions.IslandCell))<=1;
            crown.gameObject.SetActive(gardenLoaded);
            if(!hasState || restored!=wasRestored)
            {
                restoredAt=hasState?clock:clock-4;
                crownFilter.sharedMesh=restored?bloomMesh:budMesh;wasRestored=restored;hasState=true;
            }
            crown.localScale=Vector3.one*(restored?Mathf.Lerp(.3f,1,Mathf.SmoothStep(0,1,(clock-restoredAt)/3)):1);

            var navigation=default(LogicalPosition);
            NavigationAvailable=active && guidanceEnabled && TryNavigationTarget(challenge,clock,player.y,out navigation);
            NavigationTarget=navigation;
            guide.gameObject.SetActive(NavigationAvailable);targetMarker.gameObject.SetActive(NavigationAvailable);
            if(NavigationAvailable)
            {
                var location=space.ToLocal(navigation.X,navigation.Y,navigation.Z);
                TargetMarkerKind=challenge.Kind;
                targetMarker.position=location;
                bool lift=challenge.Kind==ObjectiveKind.DiscoverLift || challenge.Kind==ObjectiveKind.Soar;
                targetFilter.sharedMesh=lift?liftMarkerMesh:challenge.Kind==ObjectiveKind.CollectSeed?seedMarkerMesh
                    :challenge.Kind==ObjectiveKind.Precision?archMarkerMesh:landingMarkerMesh;
                // The seed aperture faces the approaching player; the arch marker
                // stays in its real crossing plane and the landing ring lies on the terrace.
                targetMarker.rotation=challenge.Kind==ObjectiveKind.CollectSeed
                    ?LookAlong(location-player):Quaternion.identity;
                targetMarker.localScale=Vector3.one;
                var direction=location-player;float distance=direction.magnitude;
                direction=distance>.001f?direction/distance:Vector3.forward;
                // Three substantial chevrons lead the current objective. A slow flow
                // toward it adds direction without blinking or an eye-attached overlay.
                float first=Mathf.Min(16,distance*.33f),length=Mathf.Min(30,distance*.4f);
                float flow=Mathf.Repeat(clock*.28f,1);
                guide.position=player+direction*(first+flow*Mathf.Min(2,length*.1f));
                guide.rotation=LookAlong(direction);
                float size=Mathf.InverseLerp(16,48,distance);
                guide.localScale=new Vector3(size,size,Mathf.Max(.01f,length/24));
                // The destination itself owns the final approach; the path sits
                // below the sightline and yields before it can cover the aperture.
                guide.gameObject.SetActive(distance>16);
            }
        }

        private bool TryNavigationTarget(FlightChallenge challenge,float clock,float playerHeight,out LogicalPosition target)
        {
            target=challenge.Target(clock);
            if(challenge.Kind!=ObjectiveKind.DiscoverLift && challenge.Kind!=ObjectiveKind.Soar)return true;
            if(wind==null || wind.Mode==WindMode.StillAir)return false;
            // Search within this actual thermal instead of sampling an artificial
            // ever-higher point which can leave the lift and erase all guidance.
            // Above the useful air, the route correctly points back into the column.
            for(int i=0;i<7;i++)
            {
                float height=i==0?Mathf.Clamp(playerHeight+20,45,challenge.RequiredAltitude+15)
                    :i==1?Mathf.Clamp(playerHeight-25,45,300):i==2?230:i==3?190:i==4?150:i==5?100:55;
                var candidate=new LogicalPosition(target.X,height,target.Z);
                if(wind.Sample(space.ToLocal(candidate.X,candidate.Y,candidate.Z),clock).y<=1.5f)continue;
                target=candidate;return true;
            }
            return false;
        }
        private static Quaternion LookAlong(Vector3 direction)=>direction.sqrMagnitude<.0001f?Quaternion.identity
            :Quaternion.LookRotation(direction,Mathf.Abs(Vector3.Dot(direction.normalized,Vector3.up))>.95f?Vector3.forward:Vector3.up);

        private static Mesh BuildSeed()
        {
            var shape=new PetalMesh();
            var ivory=new Color(.98f,.91f,.65f);var coral=new Color(.98f,.42f,.24f);
            for(int i=0;i<3;i++)
            {
                float a=i*Mathf.PI*2/3;var direction=new Vector3(Mathf.Cos(a),.23f,Mathf.Sin(a)).normalized;
                shape.Petal(Vector3.zero,direction,4,1.05f,1,ivory,coral);
            }
            shape.Petal(Vector3.down*1.5f,Vector3.up,3,.9f,0,coral,ivory);
            shape.Bar(Vector3.down*.85f,Vector3.up*.85f,.95f,WayGold);
            shape.Ring(Vector3.zero,Vector3.right,Vector3.up,1.8f,.14f,WayGold,12);
            return shape.Build("Three-winged story seed");
        }
        private static Mesh BuildCrown(bool bloom)
        {
            var shape=new PetalMesh();int count=bloom?14:5;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/(bloom?7:5)+(i>=7?.35f:0);
                var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                var direction=bloom?(radial+Vector3.up*(i>=7?.45f:.04f)).normalized:(Vector3.up+radial*.22f).normalized;
                shape.Petal(bloom?Vector3.up*(i>=7?2:0):radial*.3f,direction,bloom?(i>=7?9:13):5,
                    bloom?(i>=7?2.2f:3.2f):.7f,bloom?3:.2f,
                    bloom?new Color(.98f,.75f,.49f):new Color(.57f,.56f,.38f),bloom?new Color(.95f,.36f,.26f):new Color(.76f,.72f,.50f));
            }
            return shape.Build(bloom?"Restored sanctuary blossom":"Dormant sanctuary bud");
        }
        private static Mesh BuildGuide()
        {
            var shape=new PetalMesh();
            for(int i=0;i<3;i++)
            {
                var tip=Vector3.forward*(i*12)+Vector3.down*2.6f;
                // A short open trail below the target. Crossed fins seen head-on
                // became a large plus sign obscuring the destination.
                for(int side=-1;side<=1;side+=2)
                {
                    shape.OutlinedBar(tip,tip+Vector3.right*(side*1.15f)-Vector3.forward*2.8f,.09f);
                }
            }
            return shape.Build("Three gold route chevrons");
        }
        private static Mesh BuildTarget(ObjectiveKind kind)
        {
            var shape=new PetalMesh();
            if(kind==ObjectiveKind.CollectSeed)
            {
                // Open aperture at the same radius as the actual swept pickup.
                shape.OutlinedRing(Vector3.zero,Vector3.right,Vector3.up,RouteHomeChapter.SeedRadius,.24f,16);
                // A tall pin is readable beyond the collectible's render range.
                // It points into the exact aperture rather than enlarging the
                // apparent capture radius to fake visibility at long distance.
                shape.OutlinedBar(Vector3.up*11,new Vector3(-6,24,0),.6f);
                shape.OutlinedBar(Vector3.up*11,new Vector3(6,24,0),.6f);
            }
            else if(kind==ObjectiveKind.Precision)
            {
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
                {
                    var corner=new Vector3(x*13,y*13,0);
                    shape.OutlinedBar(corner,corner-Vector3.right*(x*6),.42f);
                    shape.OutlinedBar(corner,corner-Vector3.up*(y*6),.42f);
                }
            }
            else if(kind==ObjectiveKind.Land)
            {
                shape.OutlinedRing(Vector3.up*.45f,Vector3.right,Vector3.forward,12,.32f,16);
                for(int i=0;i<4;i++)
                {
                    var radial=Quaternion.Euler(0,i*90,0)*Vector3.forward;
                    shape.OutlinedBar(radial*12+Vector3.up*5,radial*12+Vector3.up*.75f,.28f);
                }
            }
            else
            {
                // Upright double arrows explicitly mean rising air; destination
                // aperture/brackets and ground ring use different silhouettes.
                for(int x=-1;x<=1;x+=2)
                {
                    var offset=Vector3.right*(x*4);
                    shape.OutlinedBar(offset-Vector3.up*3,offset+Vector3.up*4,.3f);
                    shape.OutlinedBar(offset+Vector3.up*4,offset+new Vector3(-2,1,0),.3f);
                    shape.OutlinedBar(offset+Vector3.up*4,offset+new Vector3(2,1,0),.3f);
                }
            }
            return shape.Build("Route target "+kind);
        }

        // Small sculpted leaves with a faceted ridge and volume. These are opaque
        // repo-native meshes, not transparent cards or downloaded/generated assets.
        private sealed class PetalMesh
        {
            private readonly List<Vector3> vertices=new List<Vector3>();
            private readonly List<int> triangles=new List<int>();
            private readonly List<Color> colors=new List<Color>();
            public void Bar(Vector3 from,Vector3 to,float halfWidth,Color color)
            {
                var forward=(to-from).normalized;
                var side=Vector3.Cross(forward,Vector3.up).normalized;
                if(side.sqrMagnitude<.1f)side=Vector3.right;
                var up=Vector3.Cross(side,forward).normalized;
                int start=vertices.Count;
                for(int end=0;end<2;end++)for(int corner=0;corner<4;corner++)
                {
                    vertices.Add((end==0?from:to)+side*((corner==0||corner==3?1:-1)*halfWidth)
                        +up*((corner<2?1:-1)*halfWidth));colors.Add(color);
                }
                int[] faces={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
                foreach(int index in faces)triangles.Add(start+index);
            }
            public void OutlinedBar(Vector3 from,Vector3 to,float width)
            {
                Bar(from,to,width*1.65f,WayDark);
                var direction=(to-from).normalized;
                var side=Vector3.Cross(direction,Vector3.up).normalized;
                if(side.sqrMagnitude<.1f)side=Vector3.right;
                var up=Vector3.Cross(side,direction).normalized;
                var a=Vector3.Lerp(from,to,.035f);var b=Vector3.Lerp(from,to,.965f);
                // Gold is on all four exterior faces of the dark bar, never
                // buried inside an opaque border or dependent on a view direction.
                for(int i=-1;i<=1;i+=2)
                {
                    Face(a-side*width+up*(i*width*1.66f),b-side*width+up*(i*width*1.66f),
                        b+side*width+up*(i*width*1.66f),a+side*width+up*(i*width*1.66f),up*i);
                    Face(a-up*width+side*(i*width*1.66f),b-up*width+side*(i*width*1.66f),
                        b+up*width+side*(i*width*1.66f),a+up*width+side*(i*width*1.66f),side*i);
                }
            }
            private void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
            {
                int start=vertices.Count;
                vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                for(int i=0;i<4;i++)colors.Add(WayGold);
                bool front=Vector3.Dot(Vector3.Cross(b-a,c-a),normal)>0;
                triangles.Add(start);triangles.Add(start+(front?1:2));triangles.Add(start+(front?2:1));
                triangles.Add(start);triangles.Add(start+(front?2:3));triangles.Add(start+(front?3:2));
            }
            public void Ring(Vector3 center,Vector3 right,Vector3 up,float radius,float width,Color color,int count)
            {
                for(int i=0;i<count;i++)
                {
                    float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
                    Bar(center+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius,
                        center+(right*Mathf.Cos(b)+up*Mathf.Sin(b))*radius,width,color);
                }
            }
            public void OutlinedRing(Vector3 center,Vector3 right,Vector3 up,float radius,float width,int count)
            {
                for(int i=0;i<count;i++)
                {
                    float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
                    OutlinedBar(center+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius,
                        center+(right*Mathf.Cos(b)+up*Mathf.Sin(b))*radius,width);
                }
            }
            public void Petal(Vector3 origin,Vector3 direction,float length,float width,float curl,Color inner,Color outer)
            {
                var side=Vector3.Cross(direction,Vector3.up).normalized;
                if(side.sqrMagnitude<.1f)side=Vector3.right;
                var normal=Vector3.Cross(side,direction).normalized;int start=vertices.Count;
                for(int i=0;i<=6;i++)
                {
                    float t=i/6f,s=Mathf.Sin(t*Mathf.PI),w=Mathf.Max(.015f,s*width);
                    var center=origin+direction*(length*t)+normal*(s*curl);
                    vertices.Add(center+side*w);vertices.Add(center+normal*w*.28f);vertices.Add(center-side*w);vertices.Add(center-normal*w*.16f);
                    for(int j=0;j<4;j++)colors.Add(Color.Lerp(inner,outer,t)*(j==3?.80f:1));
                    if(i==0)continue;
                    for(int j=0;j<4;j++)
                    {
                        int a=start+(i-1)*4+j,b=start+(i-1)*4+(j+1)%4,c=start+i*4+j,d=start+i*4+(j+1)%4;
                        triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);
                    }
                }
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);triangles.Add(start);triangles.Add(start+2);triangles.Add(start+3);
                int end=start+24;triangles.Add(end);triangles.Add(end+2);triangles.Add(end+1);triangles.Add(end);triangles.Add(end+3);triangles.Add(end+2);
            }
            public Mesh Build(string name)
            {
                var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            }
        }
        private void Rebase(Vector3 delta)
        {
            if(root!=null)foreach(Transform child in root)child.position-=delta;
            previousPlayer-=delta;pickupOrigin-=delta;
        }
        private void OnDisable()
        {
            NavigationAvailable=false;
            if(root!=null)root.gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            if(space!=null)space.Rebased-=Rebase;
            if(root!=null)Destroy(root.gameObject);
            foreach(var mesh in new[]{seedMesh,budMesh,bloomMesh,guideMesh,liftMarkerMesh,seedMarkerMesh,archMarkerMesh,landingMarkerMesh})if(mesh!=null)Destroy(mesh);
        }
    }
}
