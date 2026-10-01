# Video script: 4:00 maximum

Structure: **hook → promise → twist 1 (data) → twist 2 (light) → the fix (live demo) → rigour → closing line.**
Judges remember the peak and the ending, so the live demo sits in the middle and the closing line is short.

Record the app on a phone with a screen recorder, the room/card shots with a second phone, and the voice-over last.
Keep on-screen text to 6 words or fewer. The posters in `docs/gallery/` work as title cards.

| Time | Picture | Voice-over (≈ 140 words/min) |
|---|---|---|
| **0:00–0:20** | Close-up: a student yawning over books. Text: **59.1%** | "Almost six in ten teenage girls in India are anaemic. They feel tired, can't focus, and most never find out, because finding out needs a blood test." |
| **0:20–0:40** | Pull down your own lower eyelid. Text: *"Can AI see it?"* | "Nurses check the inside of your lower eyelid: pale can mean anaemic. AI apps claim a phone can do this, and the biggest public dataset reports 85% accuracy. So I tried to build one." |
| **0:40–1:25** | `poster-audit.png`, then the Science page table of 11 records. Then DupeScope scanning. Text: **46% copies** | "First I checked the data. The same photo was filed as eleven different children, with haemoglobin from 4.6 to 10.9. A model that just memorises photos scores 71%. I built a tool, DupeScope, to catch this in any dataset, and it found re-cropped copies too, some inside my own test split. So I fixed my own pipeline and re-ran everything. Honest accuracy: an AUC of 0.65." |
| **1:25–1:55** | Lighting Lab, live: red sticker + card under a tube light, then a yellow bulb. Raw vs corrected numbers. Text: **Bulb > blood** | "Second problem: light. Healthy and anaemic eyelids differ by under four units of redness. A yellow bulb shifts it by fifteen, more than four times the disease. The fix costs one rupee: a white card in the same light, so the app can cancel the light's colour. On my phone it removes [your real number]% of the error." |
| **1:55–3:00** | **Airplane mode on.** Scan the card's QR → app opens. Photo with card → "Card found automatically" → one tap on eyelid → result. Add a second photo → average. Tick a symptom. Switch to Malayalam. Show the Anaemia Mukt Bharat card. | "This is PaleCheck. It works offline and no photo ever leaves your phone. It finds the card by itself; I tap my eyelid once. I get a pallor index and where I sit among 378 real eyes, including the zone where healthy and anaemic eyes look the same. A second photo makes it steadier. If I'm tired or breathless, it tells me to get tested whatever the photo says, for free, at school, under Anaemia Mukt Bharat. In Malayalam, Hindi or English." |
| **3:00–3:35** | Quick cuts: protocol v1→v2 table, ROC, leave-one-hospital-out, GitHub Actions run with green checks. | "Under the hood: a model small enough to run in a browser, a lockbox test set, nested cross-validation, hospitals the model never saw, 28 tests proving the app computes exactly what the research validated, and CI that re-downloads the public data and re-derives every number on each change." |
| **3:35–4:00** | Back to you holding the card. Text: **Better data, not bigger AI.** | "Phone anaemia screening isn't held back by AI. It's held back by data and light. The fix starts with a one-rupee card and a dataset collected honestly. That's what PaleCheck is for." |

## Shot checklist

- [ ] Your own eyelid only (or a family member who agrees). Never film anyone without consent.
- [ ] Print the card on matte paper; check that the QR opens the app.
- [ ] Lighting Lab: the same red target under at least 3 lights (window daylight, tube light, yellow bulb).
- [ ] Airplane-mode icon visible during the demo (open the app once online first, so it is cached).
- [ ] Malayalam switch visible for 2–3 seconds.
- [ ] Final cut ≤ 3:58.
