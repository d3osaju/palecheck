# PaleCheck

[![ci](https://github.com/d3osaju/palecheck/actions/workflows/ci.yml/badge.svg)](https://github.com/d3osaju/palecheck/actions/workflows/ci.yml)

**A phone, a ₹1 printed card, and an honest look at eyelid pallor.** Live: **https://palecheck.zetalabs.in**

Nurses screen for anaemia by pulling down the lower eyelid and judging how pale it is. PaleCheck measures
that colour with a phone camera and cancels the room light using a printed white card. It runs entirely
offline in the browser; photos never leave the device.

While building it we found that the data and the light bulb matter more than any AI model:

| Finding | Number |
|---|---|
| Images in the largest public eyelid dataset (CP-AnemiC, 710) that are copies of another record's photo | **324 (45.6%)**: 312 pixel-identical in 87 groups + 12 re-cropped copies found by DupeScope |
| Accuracy a pure photo-memoriser gets under the usual random 5-fold CV | **70.7%** |
| Honest accuracy on the 386 trustworthy images (nested CV / lockbox opened once) | **AUC 0.65** / **0.65** (95% CI 0.55–0.75) |
| How much a warm bulb shifts eyelid redness, relative to the healthy-vs-anaemic gap | **4.2×** |
| Same eyelid, warm bulb vs daylight, pallor index after card correction (synthetic test) | **25 vs 25** |

PaleCheck reports a **pallor index** with an explicit *overlap zone*, never a diagnosis, and always points
to a free blood test (Anaemia Mukt Bharat).

## What's in the app

| Page | What it does |
|---|---|
| **Check** | Photo → card found automatically → one tap on the eyelid (Smart select) → pallor index, average over up to 5 photos, where you sit among 378 reference eyes, symptom checklist, next steps. English, Malayalam, Hindi. `/check?sample=3` runs a research sample in one tap. |
| **Lighting Lab** | Photograph the same target under different lights; see redness before and after card correction. CSV export. |
| **Science** | Dataset audit, protocol v1 → v2, lockbox / nested CV / leave-one-hospital-out results, ROC, lighting simulation, limitations, methods. All read from `model/out/metrics.json`. |
| **DupeScope** | Checks *any* image dataset for identical and near-identical copies, with metadata conflicts, in the browser. |
| **Card** | Printable calibration cards with a QR code to the app. |
| **History** | Results stored only in this browser; add a lab Hb for the research protocol; CSV export (numbers only). |

## Run it

```bash
dotnet run --project src/PaleCheck.Web --launch-profile http
```

Open http://localhost:5049. Production build: `bash build.sh` (publishes to `dist/` and adds a hashed CSP).
Deployed on Vercel from `main` (`vercel.json`).

## Reproduce the research

```bash
python -m pip install -r model/requirements.txt
python model/get_data.py           # downloads CP-AnemiC (8.3 MB, CC BY 4.0), checks SHA-256
python model/audit_duplicates.py   # lists every group of identical images with its conflicting records
python model/near_duplicates.py    # re-cropped / re-encoded copies
python model/train.py              # audit, validation, lighting simulation; writes model + metrics into the app
dotnet test                        # C# re-implementation must match Python to 1e-9
```

CI does all of this on every push and fails if any headline number drifts (`model/check_metrics.py`).

Protocol v2 (deterministic, seed 20261001):

1. Exclude every image whose photo appears under more than one record: pixel-identical, or near-identical
   (difference hash ≤ 6 bits, aspect within 5%, mean pixel difference ≤ 6/255).
2. Hold out 8 demo images (the app's samples, never used anywhere else).
3. Split the rest 70/30 into development and lockbox, stratified by label.
4. On development only: choose the feature set and regularisation by nested cross-validation.
5. Evaluate once on the lockbox.
6. Retrain on development + lockbox for the app; thresholds from nested out-of-fold predictions.

v1 removed only pixel-identical copies (lockbox AUC 0.68, CI 0.58–0.78). DupeScope then found near-identical
copies, 3 pairs of which straddled our own development/lockbox split, so v2 re-ran everything. Both are reported.

## Layout

```
model/                 Python: get_data, audit_duplicates, near_duplicates, features, train, check_metrics, make_assets
model/out/             model.json + metrics.json (also copied into the app)
data/clean/            Manifests: 386 reliable images (with split) and 324 excluded images grouped
src/PaleCheck.Core/    C#: colour maths, features, model, quality checks (mirrors features.py)
src/PaleCheck.Web/     Blazor WebAssembly PWA
tests/                 xUnit: Python parity fixtures, quality gates, card correction
tools/                 CSP injection for the published site, poster/screenshot renderer
docs/                  Devpost write-up, video script, experiment + usability protocols, gallery images
```

## Limitations

- Reference eyes are children aged 6–60 months in Ghana; anaemia there is Hb < 11 g/dL. Teenagers need re-validation.
- The training photos have no colour reference; the app applies card correction to photos the model was not trained on.
- Lighting results so far are simulated or synthetic; real-camera results come from the Lighting Lab protocol.
- Not a medical device. Not clinically validated.

## Credits

- Data: Appiahene P., Chaturvedi K., Asare J.W., Donkoh E.T., Prasad M. (2023). *CP-AnemiC: A conjunctival pallor dataset
  and benchmark for anemia detection in children.* Medicine in Novel Technology and Devices. Mendeley Data,
  DOI [10.17632/m53vz6b7fx.1](https://data.mendeley.com/datasets/m53vz6b7fx/1), CC BY 4.0. Changes: copies excluded.
- Prevalence: NFHS-5 (2019–21), via Scott et al., *Maternal & Child Nutrition* (2022).
- Planckian locus fit: Kang et al. (2002), *J. Korean Physical Society*.
