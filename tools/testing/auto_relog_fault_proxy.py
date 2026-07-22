#!/usr/bin/env python3
"""TCP proxy for repeatable Auto Relog connection-loss tests."""

from __future__ import annotations

import argparse
import asyncio
import socket
import struct
import time
from contextlib import suppress


class FaultProxy:
    def __init__(
        self,
        upstream_host: str,
        upstream_port: int,
        drop_after: float,
        outage_seconds: float,
        cycles: int,
        reset_connection: bool,
    ) -> None:
        self.upstream_host = upstream_host
        self.upstream_port = upstream_port
        self.drop_after = drop_after
        self.outage_seconds = outage_seconds
        self.remaining_cycles = cycles
        self.reset_connection = reset_connection
        self.outage_until = 0.0
        self.state_lock = asyncio.Lock()

    async def handle_connection(
        self,
        client_reader: asyncio.StreamReader,
        client_writer: asyncio.StreamWriter,
    ) -> None:
        peer = client_writer.get_extra_info("peername")
        if time.monotonic() < self.outage_until:
            print(f"reject peer={peer} reason=outage", flush=True)
            await self.close_writer(client_writer)
            return

        try:
            server_reader, server_writer = await asyncio.open_connection(
                self.upstream_host,
                self.upstream_port,
            )
        except OSError as exception:
            print(f"reject peer={peer} reason=upstream error={exception}", flush=True)
            await self.close_writer(client_writer)
            return

        async with self.state_lock:
            inject_fault = self.remaining_cycles != 0
            if self.remaining_cycles > 0:
                self.remaining_cycles -= 1

        print(f"connected peer={peer} inject_fault={inject_fault}", flush=True)
        drop_task = (
            asyncio.create_task(self.drop_connection(client_writer, server_writer))
            if inject_fault
            else None
        )

        relays = [
            asyncio.create_task(self.relay(client_reader, server_writer)),
            asyncio.create_task(self.relay(server_reader, client_writer)),
        ]
        try:
            await asyncio.wait(relays, return_when=asyncio.FIRST_COMPLETED)
        finally:
            for task in relays:
                task.cancel()
            if drop_task is not None:
                drop_task.cancel()
            await asyncio.gather(*relays, return_exceptions=True)
            if drop_task is not None:
                await asyncio.gather(drop_task, return_exceptions=True)
            await self.close_writer(client_writer)
            await self.close_writer(server_writer)

    async def drop_connection(
        self,
        client_writer: asyncio.StreamWriter,
        server_writer: asyncio.StreamWriter,
    ) -> None:
        await asyncio.sleep(self.drop_after)
        async with self.state_lock:
            self.outage_until = max(
                self.outage_until,
                time.monotonic() + self.outage_seconds,
            )

        mode = "reset" if self.reset_connection else "graceful"
        print(f"drop mode={mode} outage_seconds={self.outage_seconds}", flush=True)
        if self.reset_connection:
            self.set_reset_on_close(client_writer)
            self.set_reset_on_close(server_writer)
        client_writer.close()
        server_writer.close()

    @staticmethod
    async def relay(
        reader: asyncio.StreamReader,
        writer: asyncio.StreamWriter,
    ) -> None:
        while data := await reader.read(64 * 1024):
            writer.write(data)
            await writer.drain()

    @staticmethod
    def set_reset_on_close(writer: asyncio.StreamWriter) -> None:
        raw_socket = writer.get_extra_info("socket")
        if raw_socket is not None:
            raw_socket.setsockopt(
                socket.SOL_SOCKET,
                socket.SO_LINGER,
                struct.pack("ii", 1, 0),
            )

    @staticmethod
    async def close_writer(writer: asyncio.StreamWriter) -> None:
        writer.close()
        with suppress(ConnectionError, OSError):
            await writer.wait_closed()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--listen-host", default="127.0.0.1")
    parser.add_argument("--listen-port", type=int, required=True)
    parser.add_argument("--upstream-host", default="127.0.0.1")
    parser.add_argument("--upstream-port", type=int, required=True)
    parser.add_argument("--drop-after", type=float, default=5.0)
    parser.add_argument("--outage-seconds", type=float, default=10.0)
    parser.add_argument(
        "--cycles",
        type=int,
        default=1,
        help="Connections to drop. Use -1 to drop every forwarded connection.",
    )
    parser.add_argument(
        "--mode",
        choices=("graceful", "reset"),
        default="graceful",
    )
    args = parser.parse_args()
    if args.drop_after < 0 or args.outage_seconds < 0 or args.cycles < -1:
        parser.error("drop and outage values must be nonnegative; cycles must be -1 or greater")
    return args


async def main() -> None:
    args = parse_args()
    proxy = FaultProxy(
        args.upstream_host,
        args.upstream_port,
        args.drop_after,
        args.outage_seconds,
        args.cycles,
        args.mode == "reset",
    )
    server = await asyncio.start_server(
        proxy.handle_connection,
        args.listen_host,
        args.listen_port,
    )
    addresses = ", ".join(str(sock.getsockname()) for sock in server.sockets or [])
    print(f"listening addresses={addresses}", flush=True)
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
