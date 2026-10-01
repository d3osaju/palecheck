# Video script: 4:00 maximum

Structure: **hook → promise → twist 1 (copies) → twist 2 (the cheat) → twist 3 (light) → the fix (live demo) → closing line.**
Judges remember the peak and the ending: the Benchmark Lab is the peak of the story, the live demo the peak of the product.

Record the app with a screen recorder, room/card shots with a second phone, voice-over last. On-screen text: 6 words or
fewer. The posters in `docs/gallery/` work as title cards.

| Time | Picture | Voice-over (≈ 140 words/min) |
|---|---|---|
| **0:00–0:20** | Close-up: a student yawning over books. Text: **59.1%** | "Almost six in ten teenage girls in India are anaemic. They feel tired, can't focus, and most never find out, because finding out needs a blood test." |
| **0:20–0:35** | Pull down your own lower eyelid. Text: *"Can AI see it?"* | "Nurses check the inside of your lower eyelid. AI apps claim a phone can do this, and the biggest public dataset reports 85% accuracy. So I tried to build one." |
| **0:35–1:15** | `poster-audit.png` → Science page, the photo with 11 records → the 710 vs 312 stats. Text: **710 records. 312 photos.** | "First I checked the data. The same photo was filed as eleven different children, with haemoglobin from 4.6 to 10.9. Then my own tools kept finding more: re-saved copies, and the same photo cut out with a different outline, even mirror images. In the end, 710 patient records hide only 312 photographs." |
| **1:15–1:50** | Benchmark Lab: press **Run the four key experiments**, bars appear. Text: **Watch a model cheat** | "Does it matter? Here's a model that knows nothing about anaemia. It just copies the answer of the most similar photo it has seen. On the published data it scores 0.88, better than the published deep learning. Remove the copies, and it collapses to a coin flip. My colour model stays at 0.65 the whole way. That's the honest number." |
| **1:50–2:10** | Lighting Lab: red sticker + card under a tube light, then a yellow bulb. Text: **Bulb > blood** | "Second problem: light. A yellow bulb shifts eyelid redness four times more than anaemia does. The fix costs one rupee: a white card in the same light. On my phone it removes [your real number]% of the error." |
| **2:10–3:10** | **Airplane mode on.** Scan the card's QR → photo → "card found automatically" → one tap on eyelid → result: colour swatches, overlap chart. Tap **Read aloud** in Malayalam. Tick a symptom. | "This is PaleCheck. Offline, and no photo ever leaves your phone. It finds the card itself; I tap my eyelid once. I see my eyelid's colour next to the average healthy and anaemic eyelid, and where I sit among real eyes, including the zone where nobody can tell. It reads the result aloud in Malayalam, Hindi or English, and if I'm tired or breathless it tells me to get the free school blood test, whatever the photo says." |
| **3:10–3:40** | Science page, screening calculator: slide prevalence to 59%. Then CI with green checks. | "And I did the maths on myself. Where anaemia is this common, letting a photo decide who gets tested would miss 69 girls in every thousand. So PaleCheck never replaces the test; it gets people to it. Every number here is re-derived from the public data by automated tests on every change." |
| **3:40–4:00** | You holding the card. Text: **Better data, not bigger AI.** | "Phone anaemia screening isn't held back by AI. It's held back by data and light. The fix starts with a one-rupee card and honest data. That's PaleCheck." |

## Shot checklist

- [ ] Your own eyelid only (or a family member who agrees). Never film anyone without consent.
- [ ] Print the card on matte paper; check that the QR opens the app.
- [ ] Benchmark Lab: record the bars appearing (or open `/benchmark?run=1`).
- [ ] Lighting Lab: the same red target under at least 3 lights.
- [ ] Airplane-mode icon visible during the demo (open the app once online first, so it is cached).
- [ ] Malayalam read-aloud audible (needs a Malayalam text-to-speech voice on the phone; Google's TTS has one).
- [ ] Final cut ≤ 3:58.
