use std::process::Command;

fn main() {
    let args: Vec<String> = std::env::args().skip(1).collect();
    let status = Command::new("wsl.exe").args(args).status();
    std::process::exit(status.map(|value| value.code().unwrap_or(1)).unwrap_or(1));
}
