using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class CourseDomainTests
    {
        private sealed class FakeClock : IMonotonicClock
        {
            public double NowSeconds { get; set; }
        }

        [Test]
        public void LegacyMothCatchKeepsVersionOneComboAndPointBehavior()
        {
            var score = new ForagingScore();
            Assert.That(score.Catch(0), Is.EqualTo(10));
            Assert.That(score.Catch(1), Is.EqualTo(20));
            Assert.That(score.Catch(2), Is.EqualTo(30));
            Assert.That(score.Catch(3), Is.EqualTo(40));
            Assert.That(score.Catch(4), Is.EqualTo(50));
            Assert.That(score.Catch(10), Is.EqualTo(10));
            Assert.That(score.Points, Is.EqualTo(160));
            Assert.That(score.Caught, Is.EqualTo(6));
            Assert.That(score.Combo, Is.EqualTo(1));
            var tally = score.Tally(CollectibleCatalog.SunMoth.StableId);
            Assert.That(tally.Count, Is.EqualTo(6));
            Assert.That(tally.AwardedValue, Is.EqualTo(160));
        }

        [Test]
        public void TypedCatchesReturnExplicitResultsAndIndependentTallies()
        {
            var score = new ForagingScore();
            var moon = score.Catch(CollectibleCatalog.MoonMoth, 0);
            var ember = score.Catch(CollectibleCatalog.EmberMoth, 1);
            var secondMoon = score.Catch(CollectibleCatalog.MoonMoth, 2);
            Assert.That(moon.AwardedValue, Is.EqualTo(20));
            Assert.That(ember.AwardedValue, Is.EqualTo(70));
            Assert.That(secondMoon.AwardedValue, Is.EqualTo(60));
            Assert.That(secondMoon.TypeCount, Is.EqualTo(2));
            Assert.That(secondMoon.TotalCaught, Is.EqualTo(3));
            Assert.That(secondMoon.TotalValue, Is.EqualTo(150));
            Assert.That(score.Tally("moon-moth").Count, Is.EqualTo(2));
            Assert.That(score.Tally("ember-moth").AwardedValue, Is.EqualTo(70));
            Assert.That(score.Tallies[0].CollectibleId, Is.EqualTo("ember-moth"));
        }

        [Test]
        public void StableCollectibleIdCannotChangeTypeAndFailureIsAtomic()
        {
            var score = new ForagingScore();
            var original = new CollectibleDefinition("fixture", "Fixture", CollectibleType.SunMoth,
                CollectibleRarity.Common, 10, CollectibleBehavior.Circle);
            var conflicting = new CollectibleDefinition("fixture", "Fixture", CollectibleType.MoonMoth,
                CollectibleRarity.Uncommon, 20, CollectibleBehavior.Dart);
            score.Catch(original, 0);
            Assert.Throws<InvalidOperationException>(() => score.Catch(conflicting, 1));
            Assert.That(score.Caught, Is.EqualTo(1));
            Assert.That(score.Points, Is.EqualTo(10));
            Assert.That(score.Combo, Is.EqualTo(1));
        }

        [Test]
        public void ObjectiveSnapshotIsPresentationNeutralAndBounded()
        {
            var snapshot = new ObjectiveProgressSnapshot("catch-moths", "Catch moths", 1, 3,
                8, 5, ObjectiveProgressUnit.Count, ObjectiveProgressState.Active);
            Assert.That(snapshot.DisplayStage, Is.EqualTo(2));
            Assert.That(snapshot.Normalized, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => new ObjectiveProgressSnapshot("Catch Moths",
                "Catch moths", 0, 1, 0, 1, ObjectiveProgressUnit.Count,
                ObjectiveProgressState.Active));
        }

        [Test]
        public void GateAndAltitudeEvaluatorsUseSweptMotion()
        {
            var gate = new CourseTaskEvaluator(CourseTaskDefinition.Gate("gate", "Gate", P(0, 5, 0), 1));
            var beforeGate = new CourseObservation(L(0, 5, -10));
            gate.Evaluate(beforeGate, false, default);
            var gateResult = gate.Evaluate(new CourseObservation(L(0, 5, 10)), true,
                beforeGate.Position);
            Assert.That(gateResult.Status, Is.EqualTo(CourseTaskStatus.Completed));

            var band = new CourseTaskEvaluator(CourseTaskDefinition.AltitudeBand("band", "Band",
                8, 12, P(0, 0, 0), 2));
            var below = new CourseObservation(L(0, 0, 0));
            band.Evaluate(below, false, default);
            var bandResult = band.Evaluate(new CourseObservation(L(0, 20, 0)), true,
                below.Position);
            Assert.That(bandResult.Status, Is.EqualTo(CourseTaskStatus.Completed));
        }

        [Test]
        public void GateRequiresForwardPlaneCrossingInsideTheSafeAperture()
        {
            var gate = new CourseTaskEvaluator(CourseTaskDefinition.Gate("gate", "Gate", P(0, 5, 0), 2));
            gate.Evaluate(new CourseObservation(L(0, 5, 1)), false, default);
            Assert.That(gate.Evaluate(new CourseObservation(L(0, 5, -1)), true, L(0, 5, 1)).Status,
                Is.EqualTo(CourseTaskStatus.Pending), "Backward flight must not score.");
            Assert.That(gate.Evaluate(new CourseObservation(L(2.1, 5, 1)), true, L(2.1, 5, -1)).Status,
                Is.EqualTo(CourseTaskStatus.Pending), "Touching the surrounding frame must not score.");
            Assert.That(gate.Evaluate(new CourseObservation(L(1.9, 5, 1)), true, L(1.9, 5, -1)).Status,
                Is.EqualTo(CourseTaskStatus.Completed));
        }

        [Test]
        public void TypedQuotaRequiresTheMatchingCourseObjectiveIdentity()
        {
            var score = new ForagingScore();
            const string objectiveId = "fixture.v1.quota";
            var evaluator = new CourseTaskEvaluator(CourseTaskDefinition.CollectibleQuota(
                "quota", "Moon quota", CollectibleCatalog.MoonMoth, 2, P(0, 0, 0), P(0, 0, 10)),
                objectiveId);
            var catches = new List<CatchResult>
            {
                score.Catch(CollectibleCatalog.SunMoth, 0),
                score.Catch(CollectibleCatalog.MoonMoth, 1),
                score.Catch(CollectibleCatalog.MoonMoth, 2, "fixture.v1.wrong"),
                score.Catch(CollectibleCatalog.MoonMoth, 3, objectiveId)
            };
            var first = evaluator.Evaluate(new CourseObservation(L(0, 0, 0),
                catchesSincePreviousObservation: catches), false, default);
            Assert.That(first.Current, Is.EqualTo(1));
            Assert.That(first.Status, Is.EqualTo(CourseTaskStatus.Pending));
            var second = evaluator.Evaluate(new CourseObservation(L(0, 0, 0),
                catchesSincePreviousObservation: new[]
                {
                    score.Catch(CollectibleCatalog.MoonMoth, 4, objectiveId)
                }), true, L(0, 0, 0));
            Assert.That(second.Status, Is.EqualTo(CourseTaskStatus.Completed));
        }

        [Test]
        public void CorridorCanCompleteOrFailAfterEntry()
        {
            var definition = CourseTaskDefinition.Corridor("path", "Path", P(0, 0, 0),
                P(0, 0, 10), 2);
            var completed = new CourseTaskEvaluator(definition);
            completed.Evaluate(new CourseObservation(L(0, 0, 0)), false, default);
            var result = completed.Evaluate(new CourseObservation(L(0, 0, 10)), true,
                L(0, 0, 0));
            Assert.That(result.Status, Is.EqualTo(CourseTaskStatus.Completed));
            Assert.That(result.Current, Is.EqualTo(10).Within(.0001));

            var failed = new CourseTaskEvaluator(definition);
            failed.Evaluate(new CourseObservation(L(0, 0, 0)), false, default);
            result = failed.Evaluate(new CourseObservation(L(3, 0, 5)), true, L(0, 0, 0));
            Assert.That(result.Status, Is.EqualTo(CourseTaskStatus.Failed));
            Assert.That(result.FailureReason, Is.EqualTo(CourseFailureReason.CorridorLeft));
        }

        [Test]
        public void TrickAndSupportedLandingRequireTheirExactSignals()
        {
            var trick = new CourseTaskEvaluator(CourseTaskDefinition.Trick("roll", "Roll",
                FlightTrick.FullRoll));
            Assert.That(trick.Evaluate(new CourseObservation(L(0, 0, 0),
                detectedTrick: FlightTrick.InvertedHold), false, default).Status,
                Is.EqualTo(CourseTaskStatus.Pending));
            Assert.That(trick.Evaluate(new CourseObservation(L(0, 0, 0),
                detectedTrick: FlightTrick.FullRoll), true, L(0, 0, 0)).Status,
                Is.EqualTo(CourseTaskStatus.Completed));

            var landing = new CourseTaskEvaluator(CourseTaskDefinition.SupportedLanding(
                "land", "Land", P(0, 0, 0), 2, 123));
            Assert.That(landing.Evaluate(new CourseObservation(L(0, 0, 0)), false, default).Status,
                Is.EqualTo(CourseTaskStatus.Pending));
            Assert.That(landing.Evaluate(new CourseObservation(L(0, 0, 0),
                supportedLanding: true, supportedSurfaceId: 999), true, L(0, 0, 0)).Status,
                Is.EqualTo(CourseTaskStatus.Pending), "A nearby terrain landing is not the finish perch.");
            Assert.That(landing.Evaluate(new CourseObservation(L(0, 0, 0),
                supportedLanding: true, supportedSurfaceId: 123), true, L(0, 0, 0)).Status,
                Is.EqualTo(CourseTaskStatus.Completed));
        }

        [Test]
        public void CatalogHasFiveStableShortCoursesAndEveryTaskKind()
        {
            var expectedIds = new[]
            {
                "moth-line", "canopy-weave", "ruin-windows", "thermal-ladder",
                "trick-and-perch"
            };
            Assert.That(CourseCatalog.All.Count, Is.EqualTo(expectedIds.Length));
            var kinds = new HashSet<CourseTaskKind>();
            for (var i = 0; i < expectedIds.Length; i++)
            {
                var course = CourseCatalog.All[i];
                Assert.That(course.StableId, Is.EqualTo(expectedIds[i]));
                Assert.That(CourseCatalog.Find(expectedIds[i]), Is.SameAs(course));
                Assert.That(course.ExpectedSeconds, Is.InRange(15, 30));
                Assert.That(course.TimeLimitSeconds, Is.InRange(15, 30));
                Assert.That(course.ContentRevision, Is.EqualTo(i==0?4:2));
                Assert.That(course.IsValid(out var error), Is.True, error);
                foreach (var task in course.Tasks)
                {
                    kinds.Add(task.Kind);
                    if (task.Kind == CourseTaskKind.SupportedLanding)
                        Assert.That(task.RequiredSurfaceId, Is.GreaterThan(0));
                }
            }
            foreach (CourseTaskKind kind in Enum.GetValues(typeof(CourseTaskKind)))
                Assert.That(kinds, Does.Contain(kind));
            Assert.That(CourseCatalog.Find("missing"), Is.Null);
        }

        [Test]
        public void IntroCourseLaunchGateIsVisibleWithoutAnExtremeOpeningClimb()
        {
            const int seed=7319;
            var course=CourseTerrainResolver.Resolve(CourseCatalog.Find("moth-line"),seed);
            double spawnAltitude=WorldTerrain.Elevation(seed,0,0)+12;
            var gate=course.Tasks[0];
            Assert.That(gate.Kind,Is.EqualTo(CourseTaskKind.Gate));
            Assert.That(gate.PointA.Z,Is.InRange(15,25));
            Assert.That(gate.PointA.Y-spawnAltitude,Is.InRange(-2,4),
                "The first timed gate must begin in the player's forward view and normal climb envelope.");
        }

        [Test]
        public void SeededCourseAnchorsResolveAboveTheRealTerrain()
        {
            const int seed = 7319;
            foreach (var authored in CourseCatalog.All)
            {
                var resolved = CourseTerrainResolver.Resolve(authored, seed);
                Assert.That(resolved.StableId, Is.EqualTo(authored.StableId));
                Assert.That(resolved.ContentRevision, Is.EqualTo(authored.ContentRevision));
                for (var i = 0; i < resolved.Tasks.Count; i++)
                {
                    var source = authored.Tasks[i];
                    var task = resolved.Tasks[i];
                    switch (task.Kind)
                    {
                        case CourseTaskKind.Gate:
                            Assert.That(task.PointA.Y - WorldTerrain.Elevation(seed, task.PointA.X, task.PointA.Z),
                                Is.EqualTo(source.PointA.Y).Within(.001), authored.StableId+" / "+task.StableId);
                            break;
                        case CourseTaskKind.SupportedLanding:
                            for (double z = -CourseTerrainResolver.LandingPadHalfExtent;
                                z <= CourseTerrainResolver.LandingPadHalfExtent;
                                z += CourseTerrainResolver.LandingTerrainSampleStep)
                                for (double x = -CourseTerrainResolver.LandingPadHalfExtent;
                                    x <= CourseTerrainResolver.LandingPadHalfExtent;
                                    x += CourseTerrainResolver.LandingTerrainSampleStep)
                                    Assert.That(task.PointA.Y - WorldTerrain.Elevation(seed,
                                            task.PointA.X + x, task.PointA.Z + z),
                                        Is.GreaterThanOrEqualTo(CourseTerrainResolver.LandingSurfaceOffset - .001),
                                        authored.StableId + " / " + task.StableId + " footprint");
                            Assert.That(task.RequiredSurfaceId, Is.EqualTo(source.RequiredSurfaceId));
                            break;
                        case CourseTaskKind.Corridor:
                            Assert.That(task.PointA.Y, Is.GreaterThan(WorldTerrain.Elevation(seed, task.PointA.X, task.PointA.Z)));
                            Assert.That(task.PointB.Y, Is.GreaterThan(WorldTerrain.Elevation(seed, task.PointB.X, task.PointB.Z)));
                            break;
                        case CourseTaskKind.AltitudeBand:
                            Assert.That(task.MinimumAltitude,
                                Is.GreaterThan(WorldTerrain.Elevation(seed, task.PointA.X, task.PointA.Z)));
                            Assert.That(task.MaximumAltitude, Is.GreaterThan(task.MinimumAltitude));
                            break;
                    }
                }
            }
        }

        [Test]
        public void RankedCourseCenterlinesReserveSeededLandmarkClearance()
        {
            // Seed 7319 generates a large spire here. It intersected three displayed routes
            // and left approximately one millimetre beside the fourth for the Dragon.
            const double spireX = -6.128;
            const double spireZ = 94.247;
            foreach (string id in new[] { "moth-line", "canopy-weave", "ruin-windows", "thermal-ladder" })
                Assert.That(CourseRouteReservation.IsRouteReserved(CourseCatalog.Find(id), spireX, spireZ), Is.True,
                    id + " must omit the blocking seeded landmark");
            Assert.That(CourseRouteReservation.IsRouteReserved(CourseCatalog.Find("trick-and-perch"), 0, 50), Is.True,
                "The versioned shorter trick route reserves its own centerline.");
            Assert.That(CourseRouteReservation.IsRouteReserved(CourseCatalog.Find("moth-line"), 50, 50),
                Is.False, "Scenery outside the reserved route must remain.");

            var previousActivity = ActivitySelection.Chosen;
            string previousCourse = ActivitySelection.ChosenCourseId;
            try
            {
                ActivitySelection.Chosen = FlightActivity.ObstacleCourse;
                ActivitySelection.ChosenCourseId = "thermal-ladder";
                Assert.That(CourseRouteReservation.IsActiveRouteReserved(spireX, spireZ), Is.True);
                ActivitySelection.Chosen = FlightActivity.FreeFlight;
                Assert.That(CourseRouteReservation.IsActiveRouteReserved(spireX, spireZ), Is.False,
                    "Normal worlds keep their generated landmarks.");
            }
            finally
            {
                ActivitySelection.Chosen = previousActivity;
                ActivitySelection.ChosenCourseId = previousCourse;
            }
        }

        [Test]
        public void QuotaLayoutsAreDeterministicTerrainSafeAndBounded()
        {
            const int seed = 7319;
            foreach (string id in new[] { "moth-line", "trick-and-perch" })
            {
                var course = CourseTerrainResolver.Resolve(CourseCatalog.Find(id), seed);
                foreach (var task in course.Tasks)
                {
                    if (task.Kind != CourseTaskKind.CollectibleQuota) continue;
                    var first = CourseCollectibleLayout.Build(task, seed);
                    var second = CourseCollectibleLayout.Build(task, seed);
                    Assert.That(first.Length, Is.EqualTo(task.RequiredCount));
                    Assert.That(second.Length, Is.EqualTo(first.Length));
                    for (int i = 0; i < first.Length; i++)
                    {
                        Assert.That(second[i].X, Is.EqualTo(first[i].X));
                        Assert.That(second[i].Y, Is.EqualTo(first[i].Y));
                        Assert.That(second[i].Z, Is.EqualTo(first[i].Z));
                        Assert.That(first[i].Y, Is.GreaterThanOrEqualTo(
                            WorldTerrain.Elevation(seed, first[i].X, first[i].Z)
                            + CourseCollectibleLayout.MinimumGroundClearance - .001));
                    }
                    if (first.Length == 1)
                    {
                        Assert.That(first[0].X, Is.EqualTo((task.PointA.X + task.PointB.X) * .5).Within(.001));
                        Assert.That(first[0].Z, Is.EqualTo((task.PointA.Z + task.PointB.Z) * .5).Within(.001));
                    }
                }
            }
            Assert.Throws<ArgumentException>(() => CourseTaskDefinition.CollectibleQuota("too-many", "Too many",
                CollectibleCatalog.SunMoth, SkyForaging.Capacity + 1, P(0, 5, 0), P(0, 5, 10)));
        }

        [Test]
        public void ThermalLadderUsesReachableBandsWithDeterministicPracticeLift()
        {
            var course=CourseTerrainResolver.Resolve(CourseCatalog.Find("thermal-ladder"),7319);
            double previousMinimum=0;
            foreach(var task in course.Tasks)
            {
                if(task.Kind!=CourseTaskKind.AltitudeBand)continue;
                Assert.That(task.MinimumAltitude,Is.GreaterThan(previousMinimum));
                Assert.That(task.MaximumAltitude,Is.LessThan(65),task.StableId+" must remain reachable in a short attempt");
                var middle=(task.MinimumAltitude+task.MaximumAltitude)*.5;
                var lift=WindField.CourseLift(course,new LogicalPosition(task.PointA.X,middle,task.PointA.Z),WindMode.Assisted);
                Assert.That(lift.y,Is.GreaterThanOrEqualTo(6f),task.StableId+" needs real upward air");
                previousMinimum=task.MinimumAltitude;
            }
            Assert.That(WindField.CourseLift(course,new LogicalPosition(200,30,200),WindMode.Assisted),Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void RuntimeFollowsCountdownOrderedTasksResultsAndRest()
        {
            var clock = new FakeClock { NowSeconds = 100 };
            var course = new CourseDefinition("fixture-course", "Fixture", 2, 3, 15, 30,
                CourseTaskDefinition.Gate("first", "First", P(0, 0, 0), 1),
                CourseTaskDefinition.Gate("second", "Second", P(0, 0, 0), 1));
            var runtime = new CourseRuntime(course, clock);
            var transitions = new List<CourseState>();
            runtime.StateChanged += transitions.Add;
            runtime.SetCalibrationReady(true);
            Assert.That(runtime.State, Is.EqualTo(CourseState.Ready));
            runtime.BeginCountdown();
            Assert.That(runtime.CountdownNumber, Is.EqualTo(3));
            clock.NowSeconds = 101;
            runtime.Tick();
            Assert.That(runtime.CountdownNumber, Is.EqualTo(2));
            clock.NowSeconds = 102;
            runtime.Tick();
            Assert.That(runtime.CountdownNumber, Is.EqualTo(1));
            clock.NowSeconds = 103;
            runtime.Tick();
            Assert.That(runtime.State, Is.EqualTo(CourseState.Running));

            runtime.Observe(new CourseObservation(L(0, 0, -1)));
            runtime.Observe(new CourseObservation(L(0, 0, 1)));
            Assert.That(runtime.State, Is.EqualTo(CourseState.Running),
                "One observation may complete only the active ordered task.");
            Assert.That(runtime.TaskIndex, Is.EqualTo(1));
            clock.NowSeconds = 103.25;
            runtime.Observe(new CourseObservation(L(0, 0, -1)));
            runtime.Observe(new CourseObservation(L(0, 0, 1)));
            Assert.That(runtime.State, Is.EqualTo(CourseState.Finished));
            Assert.That(runtime.Progress.State, Is.EqualTo(ObjectiveProgressState.Completed));

            var key = new CourseResultKey(course, "magpie", "beginner", "still", "default");
            var result = runtime.BuildResult(key);
            Assert.That(result.Completed, Is.True);
            Assert.That(result.Ranked, Is.True);
            Assert.That(result.DurationSeconds, Is.EqualTo(.25).Within(.0001));
            runtime.ShowResults();
            runtime.BeginRest();
            runtime.CompleteRest(true);
            Assert.That(runtime.State, Is.EqualTo(CourseState.Ready));
            Assert.That(transitions, Is.EqualTo(new[]
            {
                CourseState.Ready, CourseState.Countdown3, CourseState.Countdown2,
                CourseState.Countdown1, CourseState.Running, CourseState.Finished,
                CourseState.Results, CourseState.Rest, CourseState.Ready
            }));
        }

        [Test]
        public void DelayedCountdownTickStartsTimingOnlyWhenFlightCanBeReleased()
        {
            var clock = new FakeClock();
            var course = new CourseDefinition("delayed-course", "Delayed", 1, 1, 15, 30,
                CourseTaskDefinition.Gate("gate", "Gate", P(0, 0, 100), 1));
            var runtime = new CourseRuntime(course, clock);
            var transitions = new List<CourseState>();
            runtime.StateChanged += transitions.Add;
            runtime.SetCalibrationReady(true);
            runtime.BeginCountdown();
            clock.NowSeconds = 4;
            runtime.Tick();
            Assert.That(runtime.State, Is.EqualTo(CourseState.Running));
            Assert.That(transitions, Is.EqualTo(new[]
            {
                CourseState.Ready, CourseState.Countdown3, CourseState.Countdown2,
                CourseState.Countdown1, CourseState.Running
            }));
            Assert.That(runtime.ElapsedSeconds, Is.Zero.Within(.0001));
            Assert.That(ObstacleCourseDirector.RankedFrameTimingValid(.05f),Is.True);
            Assert.That(ObstacleCourseDirector.RankedFrameTimingValid(.051f),Is.False,
                "A frame that advances more real time than simulation must become practice-only.");
        }

        [Test]
        public void UnsafeFramesInvalidateRankingAndBreakSweptContinuity()
        {
            var clock = new FakeClock();
            var course = new CourseDefinition("safe-course", "Safety", 1, 1, 15, 30,
                CourseTaskDefinition.Gate("gate", "Gate", P(0, 0, 0), 1));
            var runtime = new CourseRuntime(course, clock);
            runtime.SetCalibrationReady(true);
            runtime.BeginCountdown();
            clock.NowSeconds = 3;
            runtime.Observe(new CourseObservation(L(0, 0, -10)));
            runtime.Observe(new CourseObservation(L(0, 0, 10), paused: true,
                invalidationReasons: CourseNonRankedReason.Reset));
            runtime.Observe(new CourseObservation(L(0, 0, 11)));
            Assert.That(runtime.State, Is.EqualTo(CourseState.Running));
            Assert.That(runtime.NonRankedReasons,
                Is.EqualTo(CourseNonRankedReason.Paused | CourseNonRankedReason.Reset));
            runtime.Observe(new CourseObservation(L(0, 0, -1)));
            runtime.Observe(new CourseObservation(L(0, 0, 1)));
            Assert.That(runtime.State, Is.EqualTo(CourseState.Finished));
            var key = new CourseResultKey(course, "magpie", "beginner", "still", "default");
            Assert.That(runtime.BuildResult(key).Ranked, Is.False);
        }

        [Test]
        public void MonotonicClockControlsTimeoutAndBackwardSamplesInvalidateRanking()
        {
            var clock = new FakeClock();
            var course = new CourseDefinition("clock-course", "Clock", 1, 1, 15, 15,
                CourseTaskDefinition.Gate("far-gate", "Far gate", P(0, 0, 100), 1));
            var runtime = new CourseRuntime(course, clock);
            runtime.SetCalibrationReady(true);
            runtime.BeginCountdown();
            clock.NowSeconds = 2;
            runtime.Tick();
            clock.NowSeconds = 1;
            runtime.Tick();
            Assert.That(runtime.NonRankedReasons & CourseNonRankedReason.ClockWentBackward,
                Is.Not.EqualTo(CourseNonRankedReason.None));
            clock.NowSeconds = 3;
            runtime.Tick();
            clock.NowSeconds = 18;
            runtime.Tick();
            Assert.That(runtime.State, Is.EqualTo(CourseState.Failed));
            Assert.That(runtime.FailureReason, Is.EqualTo(CourseFailureReason.TimeLimit));
            Assert.That(runtime.ElapsedSeconds, Is.EqualTo(15).Within(.0001));
            clock.NowSeconds=1800;
            Assert.That(runtime.ElapsedSeconds,Is.EqualTo(15).Within(.0001),
                "A delayed/suspended clock must not inflate the recorded timeout duration.");
        }

        [Test]
        public void CollisionInvalidatesRankingBeforeGateEvaluation()
        {
            var clock = new FakeClock();
            var course = new CourseDefinition("collision-course", "Collision", 1, 1, 15, 30,
                CourseTaskDefinition.Gate("gate", "Gate", P(0, 0, 0), 2));
            var runtime = new CourseRuntime(course, clock);
            runtime.SetCalibrationReady(true);runtime.BeginCountdown();clock.NowSeconds=3;
            runtime.Observe(new CourseObservation(L(0,0,-1)));
            runtime.Observe(new CourseObservation(L(0,0,1),
                invalidationReasons:CourseNonRankedReason.Collision));
            Assert.That(runtime.State,Is.EqualTo(CourseState.Running),"A colliding frame cannot also complete the gate.");
            Assert.That(runtime.NonRankedReasons&CourseNonRankedReason.Collision,Is.Not.EqualTo(CourseNonRankedReason.None));
        }

        [Test]
        public void InvalidatedFrameStillConsumesDiscreteObjectiveEventForPractice()
        {
            var clock=new FakeClock();
            var task=CourseTaskDefinition.CollectibleQuota("crown-moth","Catch the Crown Moth",
                CollectibleCatalog.CrownMoth,1,P(0,10,10),P(0,10,20));
            var course=new CourseDefinition("event-course","Event Course",1,1,15,30,task);
            var runtime=new CourseRuntime(course,clock);
            runtime.SetCalibrationReady(true);runtime.BeginCountdown();clock.NowSeconds=3;
            var objectiveId=CourseObjectiveIdentity.For(course,task);
            var caught=new ForagingScore().Catch(CollectibleCatalog.CrownMoth,0,objectiveId);

            runtime.Observe(new CourseObservation(L(0,10,10),
                catchesSincePreviousObservation:new[]{caught},
                invalidationReasons:CourseNonRankedReason.FrameTiming));

            Assert.That(runtime.State,Is.EqualTo(CourseState.Finished),
                "A caught one-shot objective must remain completable in a practice attempt.");
            Assert.That(runtime.NonRankedReasons&CourseNonRankedReason.FrameTiming,
                Is.Not.EqualTo(CourseNonRankedReason.None));
            Assert.That(runtime.BuildResult(new CourseResultKey(course,"duck","beginner","still","default")).Ranked,
                Is.False);
        }

        [Test]
        public void FrameHitchInvalidatesRankingButStillCompletesCrossedGate()
        {
            var clock=new FakeClock();
            var course=new CourseDefinition("hitch-course","Hitch Course",1,1,15,30,
                CourseTaskDefinition.Gate("gate","Gate",P(0,0,0),2));
            var runtime=new CourseRuntime(course,clock);
            runtime.SetCalibrationReady(true);runtime.BeginCountdown();clock.NowSeconds=3;
            runtime.Observe(new CourseObservation(L(0,0,-1)));

            runtime.Observe(new CourseObservation(L(0,0,1),
                invalidationReasons:CourseNonRankedReason.FrameTiming));

            Assert.That(runtime.State,Is.EqualTo(CourseState.Finished),
                "A render hitch may make the attempt practice-only, but cannot discard an authoritative gate crossing.");
            Assert.That(runtime.NonRankedReasons&CourseNonRankedReason.FrameTiming,
                Is.Not.EqualTo(CourseNonRankedReason.None));
            Assert.That(runtime.BuildResult(new CourseResultKey(course,"duck","beginner","still","default")).Ranked,
                Is.False);
        }

        [Test]
        public void SupportedLandingCanCompleteAfterWalkingInFromPadEdge()
        {
            var landing=CourseTaskDefinition.SupportedLanding("finish","Finish",P(0,2,0),6,4242);
            var evaluator=new CourseTaskEvaluator(landing);
            Assert.That(evaluator.Evaluate(new CourseObservation(L(5,2,5),supportedLanding:true,
                supportedSurfaceId:4242),false,default).Status,Is.EqualTo(CourseTaskStatus.Pending));
            Assert.That(evaluator.Evaluate(new CourseObservation(L(4,2,4),supportedLanding:true,
                supportedSurfaceId:4242),true,L(5,2,5)).Status,Is.EqualTo(CourseTaskStatus.Completed));
        }

        [Test]
        public void ResultKeySeparatesContentAndScoringRevisions()
        {
            var course = new CourseDefinition("versioned", "Versioned", 4, 7, 15, 20,
                CourseTaskDefinition.Gate("gate", "Gate", P(0, 0, 0), 1));
            var key = new CourseResultKey(course, "duck", "acrobatic", "touring", "comfort");
            Assert.That(key.StorageKey, Is.EqualTo(
                "VoarVR.CourseResult.v1/versioned/content-4/score-7/duck/acrobatic/touring/comfort"));
            Assert.That(key.IsValid, Is.True);
            Assert.Throws<InvalidOperationException>(() => _ = default(CourseResultKey).StorageKey);
            var changed = new CourseDefinition("versioned", "Versioned", 4, 8, 15, 20,
                CourseTaskDefinition.Gate("gate", "Gate", P(0, 0, 0), 1));
            Assert.That(key.Matches(changed), Is.False);
        }

        private static CoursePoint P(double x, double y, double z) => new CoursePoint(x, y, z);
        private static LogicalPosition L(double x, double y, double z) => new LogicalPosition(x, y, z);
    }
}
