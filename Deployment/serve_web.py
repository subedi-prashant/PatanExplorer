import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class UnityWebHandler(SimpleHTTPRequestHandler):
    def end_headers(self):
        requestPath = self.path.partition("?")[0]
        if requestPath.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()

    def guess_type(self, path):
        if path.endswith(".wasm.br"):
            return "application/wasm"
        if path.endswith(".js.br"):
            return "application/javascript"
        if path.endswith(".data.br"):
            return "application/octet-stream"
        return super().guess_type(path)


def ParseArguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8080)
    return parser.parse_args()


def Main():
    arguments = ParseArguments()
    directory = arguments.directory.resolve()
    handler = partial(UnityWebHandler, directory=str(directory))
    server = ThreadingHTTPServer((arguments.host, arguments.port), handler)
    print(f"Serving {directory} at http://{arguments.host}:{arguments.port}", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    Main()
