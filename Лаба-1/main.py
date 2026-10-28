from collections import Counter
from pathlib import Path


STUDENT_NAME = "Ramanchuk Viachaslau"

SEQUENCES_FILE = "sequences.txt"
COMMANDS_FILE = "commands.txt"
OUTPUT_FILE = "genedata.txt"

SEPARATOR = "-" * 74


def decode_rle(sequence: str) -> str:
    result = []
    i = 0

    while i < len(sequence):
        if sequence[i].isdigit():
            count = int(sequence[i])
            if i + 1 >= len(sequence):
                result.append(sequence[i])
                i += 1
                continue
            result.append(sequence[i + 1] * count)
            i += 2
        else:
            result.append(sequence[i])
            i += 1

    return "".join(result)


def encode_rle(sequence: str) -> str:
    if not sequence:
        return ""

    result = []
    i = 0
    while i < len(sequence):
        j = i + 1
        while j < len(sequence) and sequence[j] == sequence[i]:
            j += 1

        count = j - i
        if count > 2:
            result.append(str(count))
            result.append(sequence[i])
        else:
            result.append(sequence[i] * count)
        i = j

    return "".join(result)


def read_proteins(filename: str) -> list[dict[str, str]]:
    proteins = []

    with open(filename, "r", encoding="utf-8-sig") as file:
        for line_number, line in enumerate(file, start=1):
            line = line.rstrip("\r\n")
            if not line.strip():
                continue

            parts = line.split("\t")
            if len(parts) < 3:
                raise ValueError(
                    f"Строка {line_number} в {filename}: "
                )

            protein_name = parts[0].strip()
            organism = parts[1].strip()
            encoded_sequence = "".join(parts[2:]).strip()

            proteins.append({
                "protein": protein_name,
                "organism": organism,
                "sequence": decode_rle(encoded_sequence),
            })

    return proteins


def find_proteins(proteins: list[dict[str, str]], name: str) -> list[dict[str, str]]:
    return [p for p in proteins if p["protein"] == name]


def command_search(proteins: list[dict[str, str]], query: str) -> list[str]:
    query = decode_rle(query.strip())
    matches = [p for p in proteins if query in p["sequence"]]

    if not matches:
        return [
            f"search   {query}",
            "organism\t\t\tprotein",
            "NOT FOUND",
        ]

    lines = [
        f"search   {query}",
        "organism\t\t\tprotein",
    ]
    for protein in matches:
        lines.append(f'{protein["organism"]}\t\t{protein["protein"]}')
    return lines


def command_diff(
    proteins: list[dict[str, str]], name1: str, name2: str
) -> list[str]:
    first = find_proteins(proteins, name1)
    second = find_proteins(proteins, name2)

    lines = [f"diff   {name1}   {name2}", "amino-acids difference:"]

    missing = []
    if not first:
        missing.append(name1)
    if not second:
        missing.append(name2)

    if missing:
        lines.append("MISSING: " + ", ".join(missing))
        return lines

    seq1 = first[0]["sequence"]
    seq2 = second[0]["sequence"]

    differences = sum(a != b for a, b in zip(seq1, seq2))
    differences += abs(len(seq1) - len(seq2))
    lines.append(str(differences))
    return lines


def command_mode(proteins: list[dict[str, str]], name: str) -> list[str]:
    matches = find_proteins(proteins, name)
    lines = [f"mode   {name}", "amino-acid occurs:"]

    if not matches:
        lines.append("MISSING: " + name)
        return lines

    sequence = matches[0]["sequence"]
    if not sequence:
        lines.append("MISSING: " + name)
        return lines

    counts = Counter(sequence)
    highest_count = max(counts.values())
    
    amino_acid = min(letter for letter, count in counts.items()
                     if count == highest_count)
    lines.append(f"{amino_acid}          {highest_count}")
    return lines


def process_commands(
    proteins: list[dict[str, str]], commands_filename: str, output_filename: str
) -> None:
    with open(commands_filename, "r", encoding="utf-8-sig") as commands, \
         open(output_filename, "w", encoding="utf-8", newline="\n") as output:

        output.write(STUDENT_NAME + "\n")
        output.write("Genetic Searching\n")

        command_number = 0
        for line_number, line in enumerate(commands, start=1):
            line = line.rstrip("\r\n")
            if not line.strip():
                continue

            parts = line.split("\t")
            operation = parts[0].strip().lower()
            params = parts[1:]

            command_number += 1

            if operation == "search" and len(params) >= 1:
                result = command_search(proteins, params[0])
            elif operation == "diff" and len(params) >= 2:
                result = command_diff(proteins, params[0].strip(),
                                      params[1].strip())
            elif operation == "mode" and len(params) >= 1:
                result = command_mode(proteins, params[0].strip())
            else:
                result = [
                    line,
                    f"ERROR: invalid command or parameters on line {line_number}",
                ]

            output.write(SEPARATOR + "\n")
            output.write(f"{command_number:03d}   " + result[0] + "\n")
            for result_line in result[1:]:
                output.write(result_line + "\n")

        output.write(SEPARATOR + "\n")


def main() -> None:
    base_dir = Path(__file__).resolve().parent
    sequences_path = base_dir / SEQUENCES_FILE
    commands_path = base_dir / COMMANDS_FILE
    output_path = base_dir / OUTPUT_FILE

    if not sequences_path.exists():
        raise FileNotFoundError(f"Не найден файл: {sequences_path}")
    if not commands_path.exists():
        raise FileNotFoundError(f"Не найден файл: {commands_path}")

    proteins = read_proteins(str(sequences_path))
    process_commands(proteins, str(commands_path), str(output_path))
    print(f"Готово. Результат записан в {output_path.name}")


if __name__ == "__main__":
    main()