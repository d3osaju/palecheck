# Draft note to the CP-AnemiC authors (optional — send only if you choose to)

Responsible practice when you find a problem in someone's dataset is to tell them privately and politely,
with evidence they can reproduce. The corresponding author's contact is in the paper
(DOI 10.1016/j.medntd.2023.100244).

---

**Subject:** Identical images under different records in CP-AnemiC (Mendeley Data m53vz6b7fx v1)

Dear Dr Appiahene and co-authors,

Thank you for publishing CP-AnemiC openly. I'm a high school student using it for a school research project
on phone-based anaemia screening, and I found something I thought you would want to know.

Hashing the pixel data of the 710 PNG images shows 87 groups of byte-identical images (312 images in total).
In every group, the copies are attached to different records in Anemia_Data_Collection_Sheet.xlsx: different
hospital, age, sex and haemoglobin. For example, Image_018, 234, 369, 411, 476, 492, 520, 525, 565, 626 and 630
share one image, with Hb ranging from 4.6 to 10.93 g/dL.

Two further checks found more images that share a photograph with another record: 12 re-encoded or resized
copies, and 245 images that are the same photograph cut out with a different outline (7 of them mirrored).
For example, Image_375 and Image_376 overlap pixel-for-pixel when aligned, but are listed with different ages,
sexes and hospitals. Altogether the 710 records appear to contain about 312 distinct photographs.

Under random 5-fold cross-validation, about 42% of test images have an identical copy in the training folds,
and a 1-nearest-neighbour model reaches AUC 0.88 without learning anything about pallor, which could affect
benchmark results. My scripts (`audit_duplicates.py`, `near_duplicates.py`) and the grouped list of affected
images are attached so you can check them.

I may have misunderstood how the images were collected. If so, I'd be grateful to learn what I missed.

Kind regards,
[Your name]
[School]
