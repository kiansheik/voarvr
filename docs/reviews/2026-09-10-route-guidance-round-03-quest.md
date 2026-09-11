# Route Home guidance — final independent Quest technical review

The final carry adjustment introduces no new technical defect found in this review. No blocker/high or required source revision remains within the inspected scope. Device acceptance remains open.

Inspected the current `JourneyPresentation` carry implementation and fresh evidence in `artifacts/reviews/route-guidance/round-03/`: parsed the **8-test affected PlayMode XML, all passed**; parsed the **46-frame runtime manifest, zero frames with flight text visible**; checked the recorded unchanged PlayerPrefs result. Visually inspected all three ordinary third-person carry views, Dragon advanced embodied carry and Magpie pickup midpoint. These are new frames after the socket/scale adjustment, with actual runtime presentation and camera code but explicitly staged positions/head samples. Other critics' round-3 conclusions were not read.

The socket is now 0.06 m inward and 0.15 m farther forward in body space. First-person settled scale remains 0.035. Third-person scale uses the current species chase-distance formula with a **1–3 multiplier clamp**; the fresh Dragon chase view shows a larger, identifiable carried ring/seed beside its head. The change updates existing transforms, creates no additional renderer, mesh, material or voice, and adds no per-frame collection allocation. This does not claim that the entire subsystem allocates zero bytes.

The revised apparent size remains bounded, and the seed is still a persistent possession cue rather than a new inventory object or physics body. Pickup state, scoring, camera pose, lifecycle gates and coordinate rebasing are unchanged. The affected tests revalidate target/carry transition, world rebase, lift/StillAir behavior and bearing lifecycle. Full **240 EditMode / 37 PlayMode** results are the preceding round-2 baseline; the final source adjustment received the focused eight-test rerun.

| Technical dimension | Final score |
| --- | --- |
| Bounded geometry/resources | 8/10 |
| Material/shader suitability | 8/10 |
| Objective/coordinate coupling | 8/10 |
| Lifecycle and regression coverage | 8/10 |
| Quiet-view contract and scope | 9/10 |
| Quest sustained performance, stereo comfort and worn readability | Unverified |

Remaining finding: **medium — unverified hardware risk**. The larger third-person seed, thin marker outlines and bearing have not been judged during worn stereo movement on the final APK. Verify both-eye rendering, rapid turns and close approach; measure representative streaming frame delivery. Fresh desktop images establish the revised geometry's visible presence, not that the wearer can identify possession reliably in motion.

Stop source iteration at this third round and complete build/device validation. No extra technical system or asset work is justified by this reassessment.
