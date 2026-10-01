# Devpost submission: PaleCheck

**Track:** Coding · **Category:** Biology/Medical and Environmental Science
**Live app:** https://palecheck.zetalabs.in
**For judges:** one-tap demo https://palecheck.zetalabs.in/check?sample=2 · Benchmark Lab https://palecheck.zetalabs.in/benchmark?run=1 · report https://palecheck.zetalabs.in/palecheck-report.pdf
**Code:** https://github.com/d3osaju/palecheck (make it public before submitting)

**Tagline (≤ 200 chars):**
I tried to build an AI that spots anaemia in an eyelid photo — and found 710 patient records hiding only 312 photographs. PaleCheck is the honest version, with a ₹1 card.

**Built with:** C#, .NET 10, Blazor WebAssembly, PWA, Python, NumPy, Pillow, xUnit, HTML Canvas, Web Speech API, GitHub Actions, Vercel

**Gallery images (in this order):** `docs/gallery/poster-hook.png`, `poster-audit.png`, `poster-cheat.png`,
`desktop-benchmark.png`, `poster-lighting.png`, `desktop-screening.png`, `phone-result.png`, `poster-honest.png`,
`phone-home.png`, then your real Lighting Lab screenshot.

---

## Inspiration

Almost 6 in 10 Indian girls aged 15–19 are anaemic (NFHS-5: 59.1%). Anaemia makes you tired, breathless and
slow to concentrate, and most people never find out because finding out needs a blood test.

Nurses pull down the lower eyelid and look at how pale it is. AI apps promise to do this with a phone, and the
largest public dataset reports up to 84.8% accuracy. I wanted to build that for teenagers in Kerala. Before
trusting any number, I checked the data myself.

## What it does

**PaleCheck** turns a phone into a colour-measuring tool for the inner eyelid:

1. Print the **₹1 card** (or use white paper); its QR code opens the app.
2. Take a photo with the card beside your eye. PaleCheck **finds the card itself**; you **tap your eyelid once**.
3. It cancels the room light using the card, rejects glare and shadow, and measures the eyelid. Up to 5 photos are
   **averaged** for a steadier reading.
4. You get a **pallor index**, **your eyelid colour beside the average healthy and anaemic eyelid**, where you sit among
   133 reference eyes (with an honest "overlap zone"), a **symptom checklist** that says "get tested" whatever the photo
   shows, and the **free Anaemia Mukt Bharat test**. It can **read the result aloud** in English, Malayalam or Hindi.

Offline, private (no photo ever leaves the phone), installable.

It also ships three research tools: the **Benchmark Lab** (watch a model cheat, live), **DupeScope** (find copies in
any image dataset) and a **screening calculator** (should a photo decide who gets tested?), plus a **technical report**.

## How I built it

- **A three-layer data audit.**
  1. Hashing pixels found **312 images** that are identical photos filed as *different children*: one photo appears
     as 11 children with Hb from 4.6 to 10.9 g/dL.
  2. My DupeScope tool found **12** re-encoded copies.
  3. Then the Benchmark Lab's memoriser kept scoring suspiciously well on "clean" data. Its nearest neighbours were
     consecutive image numbers: **the same photograph cut out with a different outline**. I built a detector that slides
     one cut-out over the other (FFT cross-correlation, both orientations) and validated it against a mirrored control.
     It found **245 more**, including 7 mirror images.

  In total **569 of 710 images (80%)** are copies. The 710 records contain only **312 photographs**.
- **Proof that copies inflate scores.** A memoriser that knows nothing about anaemia scores **AUC 0.88** on the published
  data, above the published deep-learning benchmark, and **0.56** once all copies are gone. My colour model stays near
  **0.65** at every level. The Benchmark Lab reproduces this live in the browser, in about 3 seconds.
- **Honest validation:** protocol fixed in advance, re-run from scratch at each audit layer (v1 → v3, all reported):
  - demo hold-out and a lockbox opened once;
  - nested cross-validation, leave-one-hospital-out and subgroups;
  - bootstrap confidence intervals.
- **Model:** L2-regularised logistic regression from scratch in NumPy on CIELAB colour features. The v3 data picked
  within-photo contrast features, which are also less sensitive to lighting.
- **Lighting physics:** blackbody re-lighting of every eyelid, then card correction (von Kries in linear RGB).
- **Screening maths:** sensitivity/specificity at every cut-off → outcomes per 1,000 people at any prevalence.
- **Engineering:**
  - 37 xUnit tests: C# matches Python to 10⁻⁹, and the Benchmark Lab engine reproduces the Python results on real data.
  - CI re-downloads the dataset and re-derives every headline number.
  - Hashed CSP; a 2.2 MB first download.

## Challenges I ran into

- **The data kept getting worse.** Every new check found another layer of copies. Each time I had to fix my own
  pipeline and re-run everything, including the part of the data I had locked away, and report all three runs.
- **The memoriser was a mystery** until I realised it was finding cut-outs of the same photograph.
- **Proving the re-crop detector doesn't over-fire.** The mirrored-pair control was the answer.
- **Lighting beats biology.** A yellow bulb moves eyelid redness 4.0× more than anaemia does.
- **Accepting the result:** an honest AUC of about 0.65 with wide uncertainty, and building a useful product around it.

## Accomplishments that I'm proud of

- Showing that 80% of a public medical-AI benchmark is copies, reproducibly, with cleaned manifests anyone can use.
- The Benchmark Lab: anyone can watch a model "cheat" and then stop cheating, in three seconds.
- Catching leaks in my *own* pipeline with my own tools, twice.
- In a synthetic test, the same eyelid scores the same under a warm bulb and in daylight after card correction. With my
  real phone: **[fill in from docs/real-camera-experiment.md]**.
- An app that says "I can't tell, get the free blood test" and explains why, in three languages, out loud.

## What I learned

- Data quality beats model choice: a model that memorises can look better than published deep learning.
- How to validate honestly, and how to report a number that got *less* certain.
- Colour science: gamma, CIELAB, white balance, and why a camera is not a colour meter.
- Screening maths: where a disease is common, a cheap test that misses cases is worse than testing everyone.

## What's next for PaleCheck

1. Real-camera Lighting Lab results across several phones and lights.
2. A calibrated dataset of Indian teenagers (consented, recent haemoglobin, card in every photo, numbers only).
3. Share the audit with the dataset authors so the benchmark can be corrected.
4. Personal tracking during the weekly blue iron-tablet programme.

---

## AI use (disclose this)

The rules say AI use for writing code/research documents is *discouraged but allowed*, and product quality is what is
judged. Say clearly which parts you did and which an AI assistant helped with (the README and report already say an AI
assistant was used), and make sure you can explain every step yourself. The judges include AI researchers.

## Judging criteria map (for you, not for pasting)

| Criterion (25% each) | Where PaleCheck earns it |
|---|---|
| Innovation & Creativity | Three-layer audit; Benchmark Lab; re-crop detector with a control; ₹1 card with auto-detection; overlap-zone design |
| Impact & Relevance (tiebreaker) | 59.1% prevalence; screening calculator → honest product positioning; free-test referral; offline, private, 3 languages, read aloud; public cleaned data |
| Execution & Technical Quality | Protocol v1→v3 transparency; nested CV + CIs; parity + reproduction tests; CI re-derives numbers; CSP; 2.2 MB |
| Presentation & Communication | One-tap demo, Benchmark Lab auto-run link, posters, report PDF, judge's tour on the home page, `video-script.md` |
