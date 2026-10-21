BUDGET BUDDY
============

Copy this whole "budget" folder onto your USB drive.

TO START
  Double-click "Start Budget Buddy.bat".
  A small window opens and the app opens in your web browser.
  Keep the small window open while you use the app.
  When you're done, click "Close app" (top right) - then it's safe to remove the drive.
  Everything is saved automatically to the "data" folder on the drive.
  Got an updated version of the app? Just reload the browser tab.

WHAT'S IN THE FOLDER
  Start Budget Buddy.bat  - starts the app (works on any Windows PC, nothing to install)
  app\budget.html         - the app
  app\server.ps1          - tiny helper that saves your data to the drive (localhost only, no internet)
  data\budget-data.json   - YOUR DATA
  data\backups\           - automatic daily backups (last 30 days)
  data\statements\        - your original statement files, filed by month (YYYY-MM)
  Budget-Bills.xlsx       - your old spreadsheet (already loaded into the app)
  examples\              - sample statements (DFCU, MCCU, Capital One)

FINDING YOUR WAY AROUND
  The bar at the top has the everyday pages: Month, Monthly Sheet, Budget, Bills,
  Savings & Debt, Income, Transactions. Everything else is under "More": Import,
  Statements, Archive, Reports, Item Tracker, Category rules, Backup & Settings.

THE MONTHLY SHEET (your spreadsheet, inside the app)
  "Monthly Sheet" tab = your old budget sheet: planned vs. actual income, costs and
  balance, active debt, emergency fund, then every section with Estimated / Actual /
  Difference / Due. Type an Actual straight into the grid.
  The Paychecks table shows what each paycheck has to cover before the next one.

PAYCHECK CALCULATOR ("🧮 Paycheck calculator" at the top right - on a phone or narrow
  window it's the round green 🧮 button, bottom right)
  Like the Calculator tab in your sheet. Pick the payday, enter the paycheck, then:
    - cards: what's already paid this month, type the balance, how much to pay
    - debts: balance and minimum - shows "Minimum paid ✓" if an earlier check already
      paid it, "$X remaining" if only part was paid, or "$X past due"; you can always
      add an extra payment
    - bills due before your next payday (ones already paid show ✓)
    - savings for this check
  It shows what's left from the paycheck as you type. "Record these payments" saves
  them as this month's actuals (debts and savings update too).

PAY SCHEDULE
  Income tab > Pay schedule: every 2 weeks from a recent payday. The app knows which
  months have 3 paychecks and counts paychecks still to come in "safe to spend".

DON'T USE STATEMENTS? TYPE YOUR TOTALS IN
  On the Month page, click "+ Add amount" (or the + next to any category) to enter
  what you paid: a bill, your total on a card, a debt payment (lowers that debt),
  money put into savings (adds to that account), or a paycheck.

NOTHING GETS COUNTED TWICE
  If you type in a payment and later import the statement that has it (same amount,
  roughly the same name, within a few days), the statement's copy replaces yours.
  The import screen shows "= your entry" so you can see it. Typing in something that's
  already in an imported statement is refused. On a credit card's own statement, the
  card payment becomes a transfer and the card's purchases count as the spending.

BILLS
  - Paid with: a bill can be on autopay to a card (e.g. Storage Unit on the Discover
    card). It then counts as part of that card's total, not twice, and shows as paid.
    Set it on the Bills tab > pencil > "Paid with".
  - Bills adjust themselves: next month expects what you've really been paying
    (average of the last 3 months). Untick "Adjust automatically" to keep it fixed,
    or change just one month by clicking the amount.
  - Category budgets made of bills follow the bills ("↺ matches bills"). Type your own
    number to override; click "↺ match bills" to switch back.
  - A bill coming in under budget shows as "under ✓" - that's money saved.

DEBTS
  - Paying extra shows in green ("+$X extra ✓") - as long as it doesn't take the
    month below zero; then you get a warning instead.
  - Paid part of a minimum? It shows the rest as "remaining, due <date>", and as
    "past due" once the due date passes.

YOUR BUDGET RUNS MONTH BY MONTH
  1. Accounts   - More > Statements > Add account. Add every checking, savings and credit
                  card account you use, from any bank (Chase, Capital One, Ally...).
                  Tip: add the last 4 digits - files with those digits in the name are
                  matched to the right account automatically.
  2. Import     - each month, drop in each bank's statement: the monthly PDF statement
                  works (e.g. DFCU), and so do CSV or OFX/QFX downloads. Drop several at
                  once. Each one is filed under its account and month (you can change the
                  month), and the original file is archived on the drive.
                  For a PDF the app checks the rows add up to the statement's ending
                  balance (for a credit card: previous balance + purchases - payments
                  = new balance). Scanned (photo) PDFs can't be read. Duplicates are skipped.
                  One PDF with several accounts in it (e.g. MCCU savings + checking) is
                  imported one account at a time - each gets its own account and check.
                  A credit card statement is filed under the month it closes in
                  (Aug 17 - Sep 15 -> September); its payments count as transfers, and
                  its purchases and cash back go under that card.
                  Nothing gets counted twice:
                    - a deposit that matches a paycheck you entered is linked to it
                    - transfers between your accounts are set aside (not spending)
                    - bills/debts/savings you typed in are matched to the statement's line
                    - if you typed in a card total (e.g. "DFCU card"), you're asked to use
                      the statement's purchases instead; they go under that card
                  Changed your mind? Import > Past imports > Undo puts everything back.
  3. Statements - see which months are in for every account ("missing" shows gaps),
                  download an original file again, or remove a statement.
  4. Sort       - pick a category for each transaction, or use "Quick sort" (keys 1-9).
                  The app remembers your choices for next time. Use the pencil icon to
                  split one purchase across categories.
                  "Find transfers" marks credit card payments so they aren't counted twice.
  5. Income     - paychecks and other income count toward the month they arrive.
  6. Budget     - set an amount per category per month.
  7. Bills      - rent, phone, insurance... see what's due each month and what's paid.
                  Also lists your subscriptions and what they cost per year.
  8. Savings & Debt
                - Savings accounts by type: emergency fund, vacation fund, general savings,
                  401(k), IRA, Roth IRA, HSA, kids accounts (Trump / 529), brokerage.
                  Set a monthly amount (plus any employer match) and an optional target.
                  For investment accounts, use "Update balance" with each statement.
                - Debts: balance, APR, minimum payment, and a payoff plan
                  (Avalanche vs Snowball).
  9. Month page - budget vs. spent, the monthly plan, bills, alerts ("Heads-up"),
                  and which statements are in for the month.
                  When the month is done, press "Next month". You'll review:
                    - debts: ending balance (fix it to match your statement) -> becomes
                      next month's starting balance
                    - savings: ending balance -> carried into next month
                    - next month's bills: same amount and due date as this month,
                      change any of them (or skip one) before you confirm
                  The month is then saved in More > Archive, exactly as it was.
                  Made a mistake? Reopen the last closed month from its page.
                  Change one bill for just one month any time: click its amount.
  9b. Archive   - every closed month: budget vs. spent, debts, savings, bills,
                  statements and your notes. Print or save it as PDF.
  10. Reports   - this month vs. last month vs. your average, cash flow, year so far.
                  "Print / Save as PDF" for a paper copy.

  Credit cards: on the Budget page, click "pick what it covers" under a card to choose
        the categories it usually pays for (e.g. Meijer card -> Groceries, Gas). The card's
        line on the Month page then uses those budgets plus the card's own "anything else"
        budget. Cards with no regular categories just use their own budget.
  401(k) / HSA: edit the account and choose "A percent of my pay". Enter your gross
        (before-tax) pay per check once; the app works out the dollars per month.
  Debts: if you correct a debt's balance, payments you already made that month still
        count. If the app missed a payment, tick "<month>'s minimum is already paid"
        in Edit debt.
  Importing: if the app says the rows are "already in" another account, pick that
        account. Otherwise everything (including paychecks) is counted twice.

  Rules: a rule can also check the amount ("over $20" / "$20 or less"), e.g.
        gas stations (Circle K, Speedway, Shell...) over $20 = Gas, $20 or less =
        Groceries (snacks and drinks from the store inside).

  Quick add: the "+ Add" button at the top (or press N) works on every page: type the
        amount, tap a category, Save. Tabs switch to debt payment, savings or income.
  Icons: every category has an icon. On the Budget page, click it to pick another
        icon or color.
  Bills calendar: the Bills page shows the month as a calendar of what's paid, what's
        due and your paydays.
  "set budget": on the Month page, a category with no budget has a "set budget" link
        that suggests your recent average.
  Light / dark: the half-moon button at the top switches between following Windows,
        light and dark (remembered on this computer).
  Keyboard: press ? for the list. N = add an amount, G then M/S/B/I/D/P/T/R jumps to
        Month, Sheet, Budget, Bills, Savings & Debt, Income, Transactions, Reports.

  Tips: in Transactions, search ">100" or "<20" to filter by amount. Names like
        "POS Withdrawal - Circlek #4706..." are shown as "Circle K" (search finds either).
        Made a mistake? Press Ctrl+Z (or the Undo button that pops up).

KEEP A COPY OFF THE DRIVE (important)
  Backup & Settings > "Full backup (.zip)" saves your data AND all archived statements
  in one .zip file in your Downloads folder. Move it to your computer or a cloud folder.
  The app reminds you if it's been more than 30 days.
  To restore: Backup & Settings > "Restore from backup..." and pick the .zip.

PASSWORD PROTECTION (recommended for a USB drive)
  Backup & Settings > Set a password. Your data AND archived statements are then
  encrypted, so anyone who finds the drive can't read them. The app locks itself after
  15 minutes without use.
  IF YOU FORGET THE PASSWORD, YOUR DATA CANNOT BE RECOVERED. Write it down somewhere safe.
  After setting it, click "Delete unprotected old backups".

NOTES
  - Only Windows is supported for saving to the drive. On a Mac you can still open
    app\budget.html in a browser, but data is then kept in that browser. Use
    "Backup & Settings > Full backup" to carry it over.
  - If Windows warns about the .bat file, choose "More info" > "Run anyway".
  - Safely eject the USB drive after closing the app.
