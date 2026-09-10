# Eurasian Magpie (Pica pica) — evidence and implementation contract

Research date: 2026-09-10. Biological dimensions use metres, kilograms and **both-wing planform area excluding the body**. Human controller travel and assistance are separate gameplay parameters. A museum wing measurement is not a wingspan.

## Evidence ledger

| Parameter | Value / convention | Status and source |
|---|---|---|
| Wing length | Mean .1933 m; .181–.211, n16; unflattened carpal joint to tip | **MEASURED**, AVONET eBird Pica pica row; European specimens |
| Tail length | Mean .2421 m; .211–.291, n8 | **MEASURED**, AVONET |
| Tarsus / culmen | .0472 / .0408 m, n8 | **MEASURED**, AVONET |
| Mass | .2175 kg | **COMPARATIVE**, AVONET's Dunning literature aggregate, not mass measured from its 16 skins |
| Primaries | 10 per wing; longest .1555–.1795 m, n12 | **MEASURED**, Featherbase feather specimens |
| Secondaries | 9–10 per wing; longest .1325–.1485 m, n14 | **MEASURED**, Featherbase; model selects 9 |
| Rectrices | 12, six graduated pairs; longest .2009–.2590 m, n11 | **MEASURED**, Featherbase |
| Wingspan modeling target | .56 m | **INFERRED**, typical Eurasian reference, not a measured specimen used here |
| Total wing area modeling target | .06171 m², geometric union of both authored wings | **INFERRED** biological geometry; measured from the authored rest artifact. Comparative P. hudsonia area .06414 m² remains a comparison, not an exact fit |
| Aspect ratio / loading | Derived b²/S and mg/S; never copied from differently defined tables | **INFERRED**, using the above modeling values |
| Bone lengths / joint axes / feather widths / tail area | Authoring assumptions constrained by silhouette and feather specimens | **INFERRED**, not anatomical measurements |
| Human leverage, cadence, stroke gain, limits, assistance, trim | Kept in gameplay tuning | **GAMEPLAY-TUNED**, never personalized into species biology |

[AVONET dataset and provenance](https://doi.org/10.6084/m9.figshare.16586228.v7), [workbook](https://ndownloader.figshare.com/files/34480856): eBird taxonomy n16 (8F/8M), Germany12/Sweden2/Norway1/Switzerland1. Its own metadata specifies **unflattened** wing. The BirdLife n22 row includes six specimens without an eBird species assignment; do not silently mix these rows. Tail and bill sample sizes differ from wing sample size.

[Featherbase](https://featherbase.info/en/species/Pica/pica): use the specimen feather tables, not its general-prose span of80–90cm. Its AVONET chord description conflicts with the primary dataset convention. Individual feather lengths do not equal projected planform dimensions. [Avon Wildlife Trust](https://www.avonwildlifetrust.org.uk/wildlife-explorer/birds/crows-and-shrikes/magpie) gives the .56m typical span used as a modeling reference.

## Flight and functional anatomy

[Tobalske & Dial 1996](https://doi.org/10.1242/jeb.199.2.263), JEB199:263–280: Montana black-billed magpies, historically Pica pica and now P. hudsonia: **COMPARATIVE**. Morphology n5, kinematics n3; mass .1583kg, span .573m, one-wing area .03207m², tail .242m. Both-wing area excludes body. Conventional b²/S=5.119 and mg/S=24.21N/m²: do not copy the transcription's inconsistent2.4N/m² or the paper's differently defined AR4.5. Tunnel4–14m/s, extended flight favoured8–10m/s, informs comparison rather than imposing a human exercise cadence.

[Hieronymus 2016](https://pmc.ncbi.nlm.nih.gov/articles/PMC5055087/), DOI10.1111/joa.12511: **COMPARATIVE pigeon** attachment anatomy. Inner primaries attach around the metacarpus, distal primaries to digits; secondaries relate to ulna and covert/ligament networks. Distal feather movement is not a rigid one-to-one function of skeletal extension. A progressive coupled fan is an approximation of these interactions, not a reconstructed magpie ligament simulation.

[Baier, Gatesy & Dial 2013](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0063982): **COMPARATIVE chukar** shoulder elevation, protraction and long-axis rotation; elbow and wrist motion differ between ascending flight and wing-assisted incline running. This justifies multiple rotational freedoms and behavior-dependent coupling; measured joint ranges cannot simply become magpie limits. No bone stretching is needed.

[Lee et al. 2015](https://www.nature.com/articles/srep09914), DOI10.1038/srep09914: paper identifies magpies as Pica pica. Four juvenile descending flights and fixed-wing tests support high-AoA lift enhancement, often stall delay, through an alula-tip vortex. Not all specimens show the same effect; alula does not guarantee reduced sink. The present project treats transfer to its European model as **COMPARATIVE** pending population/taxonomic verification. Automatic deployment and a small bounded stall-angle change are **GAMEPLAY-TUNED**; no direct upward acceleration or energy injection.

[Laura Mathews wing mechanism](https://www.lauramathewsart.com/product-page/translucent-wing-set): **DESIGN INSPIRATION ONLY**. Reciprocal joints, tension/linkage coordination, individually constrained feathers and movable alula suggest legible operator-to-wing coupling. Artwork dimensions are not bird measurements. Build an original model; do not reproduce the artwork.

## Rig and physics boundary

Current Duck/Dragon assets have nine core wing/root bones plus four leg/foot bones. They have analytic two-link targeting and wrist twist but no independently controlled remiges, alula or rectrices. Preserve their established mapping. A separate avian presentation can add shoulder sweep/rotation, nonlinear manus fold, progressive primary fan and forearm-attached secondary overlap; membrane and future insect systems must not inherit that skeleton.

Magpie budget: nine core bones +20 primaries +18 secondaries +12 rectrices +2 alula +2 feet =63. Alula's few small feathers may share one joint per wing. No per-feather aerodynamic solver: coupled visual states drive a documented aggregate tail drag/turn response and alula stall adjustment. Biology, authoring inference and human assistance remain separate. Visual fidelity, aerodynamic measurements and Quest performance remain pending until verified on the implemented artifact.

## Artifact reconciliation, revision 2

Rest-pose mesh XY projected union (coverts/remiges, separate body/tail excluded) is .05296m²; span .56000m, AR5.92 and model loading40.29N/m². Scanline strips .5/.25/.125mm converge to .0529579/.0529587/.0529590m². This is measured **model geometry**, not measured Eurasian wing area. Inferred humerus/forearm/manus lengths .05582/.06773/.04232m; longest primary .15669m, secondary .14200m, central tail .24210m. Runtime embodiment scale2.5 is explicitly gameplay-only. The model now uses dark remiges and white inner primary vanes. Exact biological wingbeat frequency remains unavailable; the optional zero field is accompanied by a false availability flag. Human1Hz exercise fixtures are gameplay tests.

Revision3 connects the inner wing and primary/secondary surface with overlapping coverts and revised primary directions. Final union area .061706m² (profile .06171), span still .56m: AR5.08, loading34.58N/m². Feather shells are .12mm thick with .5mm stacking increments and shallow camber to avoid intersection flicker. Feet pivot at the sole for existing ground placement. These remain inferred authoring choices.
